using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Correção de uma venda já fechada: o caixa marcou "Dinheiro" e o cliente pagou no cartão,
/// esqueceu o CPF no cupom, digitou o parcelamento errado.
///
/// O que a correção NÃO faz é tão importante quanto o que ela faz: itens, desconto e total
/// ficam de fora, então o pagamento continua sendo conferido contra o mesmo valor de sempre
/// e a baixa de estoque nunca é tocada.
/// </summary>
public class EditarVendaTests
{
    private readonly Mock<IVendaRepository> _vendaRepository = new();
    private readonly Mock<IClienteRepository> _clienteRepository = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();

    private VendaService CriarServico()
    {
        // O repositório devolve a venda já com as alterações aplicadas em memória — é o que
        // AtualizarDadosGeraisAsync faz de verdade depois do commit.
        _vendaRepository
            .Setup(r => r.AtualizarDadosGeraisAsync(It.IsAny<Venda>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Venda venda, CancellationToken _) => venda);

        var usuarioAtual = new Mock<IUsuarioAtual>();
        usuarioAtual.SetupGet(u => u.Nome).Returns("Caixa 1");

        return new VendaService(
            _vendaRepository.Object,
            new Mock<IProdutoRepository>().Object,
            _clienteRepository.Object,
            new Mock<IDatabaseHealthService>().Object,
            new Mock<IContingencyVendaService>().Object,
            _auditoria.Object,
            usuarioAtual.Object,
            NullLogger<VendaService>.Instance);
    }

    private Venda RegistrarVendaNoRepositorio(
        decimal total = 100m,
        StatusVenda status = StatusVenda.Finalizada)
    {
        var venda = new Venda
        {
            Id = 7,
            NumeroVenda = "000007",
            Status = status,
            Subtotal = total,
            Total = total,
            FormaPagamento = FormaPagamento.Dinheiro,
            QuantidadeParcelas = 1,
            ValorPago = total,
            NomeCompradorCupom = "Consumidor Final",
            Itens = [new ItemVenda { Id = 1, ProdutoId = 1, Quantidade = 1m, PrecoUnitario = total, Subtotal = total }],
            Pagamentos =
            [
                new VendaPagamento { Id = 1, VendaId = 7, FormaPagamento = FormaPagamento.Dinheiro, Valor = total, Ordem = 1 }
            ]
        };

        _vendaRepository.Setup(r => r.ObterComItensAsync(venda.Id)).ReturnsAsync(venda);
        return venda;
    }

    private static AtualizarVendaDto Correcao(decimal valor, FormaPagamento forma, int parcelas = 1) => new()
    {
        Id = 7,
        ConsumidorFinal = true,
        Pagamentos =
        [
            new CriarVendaPagamentoDto { FormaPagamento = forma, Valor = valor, QuantidadeParcelas = parcelas }
        ]
    };

    [Fact]
    public async Task TrocarDinheiroPorCartao_GravaAFormaEAsParcelasCorrigidas()
    {
        var venda = RegistrarVendaNoRepositorio(total: 100m);
        var servico = CriarServico();

        var resultado = await servico.AtualizarAsync(Correcao(100m, FormaPagamento.CartaoCredito, parcelas: 3));

        Assert.Equal(FormaPagamento.CartaoCredito, resultado.FormaPagamento);
        Assert.Equal(3, resultado.QuantidadeParcelas);
        Assert.Equal(FormaPagamento.CartaoCredito, venda.Pagamentos.Single().FormaPagamento);
    }

    /// <summary>
    /// O total da venda não é campo de edição: a conferência do pagamento usa o valor que já
    /// está gravado. Pagamento a menos deixaria uma venda de 100 com 60 recebidos.
    /// </summary>
    [Fact]
    public async Task PagamentoQueNaoCobreOTotalDaVenda_EhRecusado()
    {
        RegistrarVendaNoRepositorio(total: 100m);
        var servico = CriarServico();

        var erro = await Record.ExceptionAsync(() => servico.AtualizarAsync(Correcao(60m, FormaPagamento.Pix)));

        Assert.IsType<DomainException>(erro);
        _vendaRepository.Verify(r => r.AtualizarDadosGeraisAsync(It.IsAny<Venda>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Venda cancelada já teve o estoque reposto e não vale mais nada — corrigir o
    /// pagamento dela só criaria um registro que se contradiz.</summary>
    [Fact]
    public async Task VendaCancelada_NaoPodeSerEditada()
    {
        RegistrarVendaNoRepositorio(status: StatusVenda.Cancelada);
        var servico = CriarServico();

        var erro = await Record.ExceptionAsync(() => servico.AtualizarAsync(Correcao(100m, FormaPagamento.Pix)));

        Assert.IsType<DomainException>(erro);
        Assert.Contains("cancelada", erro!.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Itens e total são o motivo de a correção não precisar mexer em estoque. Se um dia
    /// passarem a mudar aqui, a venda ficaria divergindo da baixa que já aconteceu.
    /// </summary>
    [Fact]
    public async Task Correcao_NaoMexeEmItensNemNoTotalDaVenda()
    {
        var venda = RegistrarVendaNoRepositorio(total: 100m);
        var servico = CriarServico();

        await servico.AtualizarAsync(Correcao(100m, FormaPagamento.Pix));

        Assert.Equal(100m, venda.Total);
        Assert.Single(venda.Itens);
        Assert.Equal(1m, venda.Itens.Single().Quantidade);
    }

    /// <summary>O CPF esquecido no fechamento é justamente um dos motivos de existir esta
    /// tela — e documento inválido tem que parar aqui, não na hora de emitir a nota.</summary>
    [Fact]
    public async Task IdentificarOCompradorDepois_GravaNomeEDocumento()
    {
        var venda = RegistrarVendaNoRepositorio(total: 100m);
        var servico = CriarServico();

        var correcao = Correcao(100m, FormaPagamento.Dinheiro);
        correcao.ConsumidorFinal = false;
        correcao.NomeCompradorAvulso = "Maria Souza";
        correcao.DocumentoCompradorAvulso = "191.191.191-00";
        correcao.TipoPessoaCompradorAvulso = TipoPessoa.Fisica;

        await servico.AtualizarAsync(correcao);

        Assert.Equal("Maria Souza", venda.NomeCompradorCupom);
        Assert.Equal("191.191.191-00", venda.DocumentoCompradorCupom);
    }

    [Fact]
    public async Task DocumentoDoCompradorInvalido_EhRecusado()
    {
        RegistrarVendaNoRepositorio(total: 100m);
        var servico = CriarServico();

        var correcao = Correcao(100m, FormaPagamento.Dinheiro);
        correcao.ConsumidorFinal = false;
        correcao.NomeCompradorAvulso = "Maria Souza";
        correcao.DocumentoCompradorAvulso = "111.111.111-11";
        correcao.TipoPessoaCompradorAvulso = TipoPessoa.Fisica;

        var erro = await Record.ExceptionAsync(() => servico.AtualizarAsync(correcao));

        Assert.IsType<DomainException>(erro);
        _vendaRepository.Verify(r => r.AtualizarDadosGeraisAsync(It.IsAny<Venda>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Mexer numa venda fechada é exatamente o tipo de coisa que alguém vai querer conferir
    /// depois. O log é o único lugar onde isso fica registrado — e precisa guardar o antes,
    /// senão não dá para saber o que foi alterado.
    /// </summary>
    [Fact]
    public async Task Edicao_FicaRegistradaNaAuditoriaComOValorAnterior()
    {
        RegistrarVendaNoRepositorio(total: 100m);
        var servico = CriarServico();

        RegistrarLogAuditoriaDto? log = null;
        _auditoria
            .Setup(a => a.RegistrarAsync(It.IsAny<RegistrarLogAuditoriaDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarLogAuditoriaDto, CancellationToken>((dto, _) => log = dto)
            .Returns(Task.CompletedTask);

        await servico.AtualizarAsync(Correcao(100m, FormaPagamento.Pix));

        Assert.NotNull(log);
        Assert.Equal("VENDA_EDITADA", log!.Acao);
        Assert.Equal(NivelLogAuditoria.Warning, log.Nivel);
        Assert.Contains("Dinheiro", log.PayloadJson);
        Assert.Contains("Pix", log.PayloadJson);
    }

    /// <summary>Correção com duas formas de pagamento: o cabeçalho da venda passa a resumir a
    /// composição, como no fechamento normal do PDV.</summary>
    [Fact]
    public async Task CorrigirParaPagamentoMisto_GravaAsDuasLinhas()
    {
        var venda = RegistrarVendaNoRepositorio(total: 100m);
        var servico = CriarServico();

        var correcao = Correcao(60m, FormaPagamento.Pix);
        correcao.Pagamentos.Add(new CriarVendaPagamentoDto
        {
            FormaPagamento = FormaPagamento.Dinheiro,
            Valor = 40m,
            ValorRecebido = 50m
        });

        await servico.AtualizarAsync(correcao);

        Assert.Equal(2, venda.Pagamentos.Count);
        Assert.Equal(100m, venda.Pagamentos.Sum(p => p.Valor));
        Assert.Equal(10m, venda.Troco);
    }
}
