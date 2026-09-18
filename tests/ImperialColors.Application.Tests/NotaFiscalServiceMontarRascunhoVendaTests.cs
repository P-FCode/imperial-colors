using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Faturamento de uma venda já registrada (botão "Emitir Nota" da tela de Vendas). O método
/// existia mas nunca tinha sido exercitado por ninguém — os casos aqui cobrem tanto as
/// guardas de negócio (venda que não pode virar nota, venda já faturada) quanto os dois
/// pontos onde a nota montada divergia da venda: desconto de cabeçalho e preço negociado.
/// </summary>
public class NotaFiscalServiceMontarRascunhoVendaTests
{
    private const int VendaId = 7;
    private const int ProdutoId = 1;
    private const decimal PrecoTabela = 100m;

    private static Venda CriarVenda(
        StatusVenda status = StatusVenda.Finalizada,
        decimal desconto = 0m,
        decimal precoPraticado = PrecoTabela,
        decimal quantidade = 1m,
        bool comPagamentos = true,
        int? clienteId = null)
    {
        var subtotal = quantidade * precoPraticado;
        var venda = new Venda
        {
            Id = VendaId,
            NumeroVenda = "000123",
            Status = status,
            ClienteId = clienteId,
            NomeCompradorCupom = "João Comprador",
            DocumentoCompradorCupom = "12345678909",
            TipoPessoaComprador = TipoPessoa.Fisica,
            Subtotal = subtotal,
            Desconto = desconto,
            Total = subtotal - desconto,
            FormaPagamento = FormaPagamento.Pix,
            QuantidadeParcelas = 1,
            Itens =
            [
                new ItemVenda
                {
                    ProdutoId = ProdutoId,
                    Quantidade = quantidade,
                    PrecoUnitario = precoPraticado,
                    Subtotal = subtotal
                }
            ]
        };

        if (comPagamentos)
            venda.Pagamentos =
            [
                new VendaPagamento { FormaPagamento = FormaPagamento.Pix, Valor = venda.Total, QuantidadeParcelas = 1, Ordem = 1 }
            ];

        return venda;
    }

    private static NotaFiscalService CriarServico(
        Venda venda,
        TributacaoProduto? tributacao = null,
        IReadOnlyList<NotaFiscal>? notasDaVenda = null)
    {
        var vendaRepo = new Mock<IVendaRepository>();
        vendaRepo.Setup(r => r.ObterComItensAsync(VendaId)).ReturnsAsync(venda);

        var notaRepo = new Mock<INotaFiscalRepository>();
        notaRepo.Setup(r => r.ListarPorVendaAsync(VendaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notasDaVenda ?? []);
        notaRepo.Setup(r => r.ObterProximoNumeroAsync(
                It.IsAny<TipoNotaFiscal>(), It.IsAny<string>(), It.IsAny<AmbienteEmissaoFiscal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("42");

        var produtoRepo = new Mock<IProdutoRepository>();
        produtoRepo.Setup(r => r.ObterPorIdAsync(ProdutoId)).ReturnsAsync(new Produto
        {
            Id = ProdutoId,
            CodigoInterno = "P001",
            Nome = "Tinta acrílica 18L",
            Unidade = "UN",
            PrecoVenda = PrecoTabela
        });

        var tributacaoRepo = new Mock<ITributacaoProdutoRepository>();
        tributacaoRepo.Setup(r => r.ObterPorProdutoIdAsync(ProdutoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tributacao);

        var configuracaoFiscal = new Mock<IConfiguracaoFiscalService>();
        configuracaoFiscal.Setup(s => s.ObterConfiguracaoEmpresaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfiguracaoFiscalEmpresaDto { Serie = "1", Uf = "PR" });
        configuracaoFiscal.Setup(s => s.ObterCodigoCrtAsync(It.IsAny<CancellationToken>())).ReturnsAsync("3");

        return new NotaFiscalService(
            notaRepo.Object,
            vendaRepo.Object,
            Mock.Of<IClienteRepository>(),
            produtoRepo.Object,
            tributacaoRepo.Object,
            Mock.Of<ITributacaoCategoriaRepository>(),
            Mock.Of<IRepository<NaturezaOperacao>>(),
            configuracaoFiscal.Object,
            Mock.Of<IFiscalApiClient>());
    }

    [Fact]
    public async Task VendaFinalizada_MontaRascunhoVinculadoAVendaComItensEPagamentos()
    {
        var servico = CriarServico(CriarVenda());

        var nota = await servico.MontarRascunhoAPartirDeVendaAsync(VendaId, TipoNotaFiscal.NFCe);

        Assert.Equal(VendaId, nota.VendaId);
        Assert.Equal(TipoNotaFiscal.NFCe, nota.Tipo);
        Assert.Equal("1", nota.Serie);
        Assert.Equal("42", nota.Numero);
        Assert.Equal(0, nota.Id); // ainda não persistida — quem grava é CriarRascunhoAsync
        var item = Assert.Single(nota.Itens);
        Assert.Equal(ProdutoId, item.ProdutoId);
        Assert.Equal(PrecoTabela, item.ValorTotal);
        Assert.Single(nota.Pagamentos);
    }

    /// <summary>Sem cliente cadastrado, o comprador informado no cupom vira o destinatário —
    /// é o que permite a NFC-e sair com CPF na nota.</summary>
    [Fact]
    public async Task VendaSemCliente_UsaCompradorDoCupomComoDestinatario()
    {
        var servico = CriarServico(CriarVenda());

        var nota = await servico.MontarRascunhoAPartirDeVendaAsync(VendaId, TipoNotaFiscal.NFCe);

        Assert.Equal("João Comprador", nota.DestinatarioNome);
        Assert.Equal("12345678909", nota.DestinatarioDocumento);
        Assert.Equal(IndicadorIeDestinatario.NaoContribuinte, nota.DestinatarioIndicadorIe);
    }

    [Theory]
    [InlineData(StatusVenda.Aberta)]
    [InlineData(StatusVenda.Cancelada)]
    public async Task VendaNaoFinalizada_Recusa(StatusVenda status)
    {
        var servico = CriarServico(CriarVenda(status));

        var erro = await Assert.ThrowsAsync<DomainException>(
            () => servico.MontarRascunhoAPartirDeVendaAsync(VendaId, TipoNotaFiscal.NFe));

        Assert.Contains("venda finalizada", erro.Message);
    }

    /// <summary>Faturar duas vezes a mesma venda é imposto em dobro sobre uma receita que só
    /// existiu uma vez — Emitindo e Indeterminada bloqueiam junto com Autorizada porque nos
    /// dois a SEFAZ pode já ter autorizado sem a resposta ter chegado de volta.</summary>
    [Theory]
    [InlineData(StatusNotaFiscal.Autorizada)]
    [InlineData(StatusNotaFiscal.Emitindo)]
    [InlineData(StatusNotaFiscal.Indeterminada)]
    public async Task VendaComNotaViva_Recusa(StatusNotaFiscal status)
    {
        var servico = CriarServico(
            CriarVenda(),
            notasDaVenda: [new NotaFiscal { Id = 9, Tipo = TipoNotaFiscal.NFe, Serie = "1", Numero = "10", Status = status }]);

        var erro = await Assert.ThrowsAsync<DomainException>(
            () => servico.MontarRascunhoAPartirDeVendaAsync(VendaId, TipoNotaFiscal.NFe));

        Assert.Contains("NF-e 1/10", erro.Message);
    }

    /// <summary>Nota cancelada/denegada não cobre mais a venda — refaturar é o procedimento
    /// correto, e um bloqueio aqui deixaria a venda sem nota para sempre. Rascunho e
    /// rejeitada também não bloqueiam: a tela oferece retomar, mas criar outra é válido.</summary>
    [Theory]
    [InlineData(StatusNotaFiscal.Cancelada)]
    [InlineData(StatusNotaFiscal.Denegada)]
    [InlineData(StatusNotaFiscal.Rascunho)]
    [InlineData(StatusNotaFiscal.Rejeitada)]
    public async Task VendaComNotaQueNaoCobreAOperacao_PermiteMontarOutra(StatusNotaFiscal status)
    {
        var servico = CriarServico(
            CriarVenda(),
            notasDaVenda: [new NotaFiscal { Id = 9, Tipo = TipoNotaFiscal.NFe, Serie = "1", Numero = "10", Status = status }]);

        var nota = await servico.MontarRascunhoAPartirDeVendaAsync(VendaId, TipoNotaFiscal.NFe);

        Assert.Equal(VendaId, nota.VendaId);
    }

    /// <summary>
    /// Regressão: o desconto da venda é de cabeçalho (Total = Subtotal − Desconto) e não
    /// estava sendo copiado para a nota. O vNF saía igual ao SUBTOTAL enquanto os pagamentos
    /// copiados somavam o total já descontado — toda venda com desconto era barrada na
    /// emissão por "A soma dos pagamentos não bate com o total da nota".
    /// </summary>
    [Fact]
    public async Task VendaComDesconto_LevaODescontoParaANotaEFechaComOTotalDaVenda()
    {
        var venda = CriarVenda(desconto: 30m);
        var servico = CriarServico(venda);

        var nota = await servico.MontarRascunhoAPartirDeVendaAsync(VendaId, TipoNotaFiscal.NFCe);

        Assert.Equal(30m, nota.VDesc);
        Assert.Equal(venda.Total, nota.VNf);
        Assert.Equal(nota.VNf, nota.Pagamentos.Sum(p => p.Valor));
    }

    /// <summary>
    /// Regressão: a tributação do item era calculada dentro de MontarItemAPartirDeProdutoAsync
    /// sobre o preço de TABELA e só depois o preço praticado na venda era escrito por cima —
    /// um item negociado a menos saía com vBC/vICMS calculados sobre o valor cheio, ou seja,
    /// imposto destacado a maior na nota que vai para a SEFAZ.
    /// </summary>
    [Fact]
    public async Task ItemComPrecoNegociado_CalculaOImpostoSobreOPrecoDaVendaNaoODeTabela()
    {
        var servico = CriarServico(
            CriarVenda(precoPraticado: 60m),
            new TributacaoProduto { ProdutoId = ProdutoId, CstIcms = "00", AliquotaIcms = 10m });

        var nota = await servico.MontarRascunhoAPartirDeVendaAsync(VendaId, TipoNotaFiscal.NFe);

        var item = Assert.Single(nota.Itens);
        Assert.Equal(60m, item.ValorTotal);
        Assert.Equal(60m, item.BaseIcms);
        Assert.Equal(6m, item.ValorIcms); // 60 x 10% — sobre 100 (tabela) sairia 10
        Assert.Equal(6m, nota.VIcms);
    }

    /// <summary>Venda anterior ao pagamento composto: forma única gravada só no cabeçalho,
    /// sem linhas em venda_pagamentos. Sem o fallback a nota nascia sem pagamento nenhum e a
    /// emissão parava em "Informe ao menos uma forma de pagamento".</summary>
    [Fact]
    public async Task VendaLegadaSemLinhasDePagamento_UsaAFormaDoCabecalhoDaVenda()
    {
        var venda = CriarVenda(comPagamentos: false);
        var servico = CriarServico(venda);

        var nota = await servico.MontarRascunhoAPartirDeVendaAsync(VendaId, TipoNotaFiscal.NFCe);

        var pagamento = Assert.Single(nota.Pagamentos);
        Assert.Equal(FormaPagamento.Pix, pagamento.FormaPagamento);
        Assert.Equal(venda.Total, pagamento.Valor);
    }
}
