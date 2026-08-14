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
/// Numeração fiscal só pode avançar quando ela é de fato gasta em uma transmissão.
///
/// O caso real: o operador clicou em "Emitir" numa nota rejeitada, o item estava com CST de
/// ICMS enquanto a empresa é Simples Nacional (CRT 1, que exige CSOSN), e a validação local
/// barrou antes de qualquer chamada à API. Só que a renumeração acontecia no TOPO do método,
/// antes de validar — então o nNF já tinha sido incrementado e gravado. O operador ia corrigir
/// a tributação em Estoque, voltava, clicava de novo, e cada tentativa que parava na validação
/// empurrava o número mais um, em silêncio e sem registrar evento. Quando a nota finalmente
/// passou na validação, o nNF já tinha caminhado vários números para frente.
/// </summary>
public class NotaFiscalEmissaoNumeracaoTests
{
    private const string NumeroOriginal = "17";

    private static NotaFiscal NotaRejeitadaComCstIncompativel() => new()
    {
        Id = 1,
        Tipo = TipoNotaFiscal.NFe,
        Status = StatusNotaFiscal.Rejeitada,
        Serie = "1",
        Numero = NumeroOriginal,
        Crt = "1",
        Ambiente = AmbienteEmissaoFiscal.Producao,
        DestinatarioNome = "Consumidor Teste",
        DestinatarioDocumento = "11144477735",
        DestinatarioIndicadorIe = IndicadorIeDestinatario.NaoContribuinte,
        DestinatarioLogradouro = "Rua Carlos Klemtz",
        DestinatarioNumero = "163",
        DestinatarioBairro = "Fazendinha",
        DestinatarioCidade = "Curitiba",
        DestinatarioCodigoMunicipioIbge = "4106902",
        DestinatarioUf = "PR",
        DestinatarioCep = "81330000",
        Itens =
        [
            new ItemNotaFiscal
            {
                Descricao = "Tinta Acrílica 18L",
                Ncm = "32091010",
                Cfop = "5102",
                ValorTotal = 100m,
                // A empresa está no Simples (CRT 1), que exige CSOSN — CST aqui é exatamente
                // o erro que o operador foi corrigir em Estoque.
                CstIcms = "00",
                CstIbsCbs = "000",
                CClassTrib = "000001",
                BaseIbsCbs = 100m
            }
        ],
        Pagamentos = [new NotaFiscalPagamento { FormaPagamento = FormaPagamento.Dinheiro, Valor = 100m }]
    };

    private static ConfiguracaoFiscalEmpresaDto Empresa() => new()
    {
        ApiKeyFiscal = "chave-de-teste",
        Cnpj = "13416624000136",
        RazaoSocial = "Imperial Colors Tintas",
        InscricaoEstadual = "9070300551",
        Logradouro = "Rua Carlos Klemtz",
        Numero = "163",
        Bairro = "Fazendinha",
        CodigoMunicipioIbge = "4106902",
        NomeMunicipio = "Curitiba",
        Cep = "81330000",
        Uf = "PR",
        Serie = "1",
        Ambiente = AmbienteEmissaoFiscal.Producao,
        ValidarNcmEmNotas = true
    };

    private static (NotaFiscalService Servico, Mock<INotaFiscalRepository> Repositorio, Mock<IFiscalApiClient> Api)
        CriarServico(NotaFiscal nota)
    {
        var repositorio = new Mock<INotaFiscalRepository>();
        repositorio.Setup(r => r.ObterPorIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        repositorio.Setup(r => r.ObterProximoNumeroAsync(
                It.IsAny<TipoNotaFiscal>(), It.IsAny<string>(), It.IsAny<AmbienteEmissaoFiscal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("18");

        var configuracao = new Mock<IConfiguracaoFiscalService>();
        configuracao.Setup(c => c.ObterConfiguracaoEmpresaAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Empresa());
        configuracao.Setup(c => c.ObterCodigoCrtAsync(It.IsAny<CancellationToken>())).ReturnsAsync("1");

        var api = new Mock<IFiscalApiClient>();

        var servico = new NotaFiscalService(
            repositorio.Object,
            Mock.Of<IVendaRepository>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<IProdutoRepository>(),
            Mock.Of<ITributacaoProdutoRepository>(),
            Mock.Of<ITributacaoCategoriaRepository>(),
            Mock.Of<IRepository<NaturezaOperacao>>(),
            configuracao.Object,
            api.Object);

        return (servico, repositorio, api);
    }

    /// <summary>
    /// O teste de regressão do bug: a validação barra, nada é transmitido, e o número
    /// continua exatamente o mesmo — pronto para a próxima tentativa depois da correção.
    /// </summary>
    [Fact]
    public async Task EmissaoBarradaNaValidacao_NaoConsomeNumeracao()
    {
        var nota = NotaRejeitadaComCstIncompativel();
        var (servico, repositorio, api) = CriarServico(nota);

        var erro = await Assert.ThrowsAsync<DomainException>(() => servico.EmitirAsync(nota.Id));

        // A mensagem tem que ser a da tributação, não uma queixa genérica — é ela que manda
        // o operador para Estoque.
        Assert.Contains("CSOSN", erro.Message);

        Assert.Equal(NumeroOriginal, nota.Numero);
        repositorio.Verify(r => r.ObterProximoNumeroAsync(
            It.IsAny<TipoNotaFiscal>(), It.IsAny<string>(), It.IsAny<AmbienteEmissaoFiscal>(), It.IsAny<CancellationToken>()),
            Times.Never);
        api.Verify(a => a.EmitirAsync(
            It.IsAny<NotaFiscal>(), It.IsAny<EmitenteFiscalDto>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// A contrapartida: quando a nota rejeitada passa na validação e vai mesmo ser
    /// transmitida, o número TEM que avançar — reenviar com o mesmo nNF é o que produz a
    /// duplicidade cStat 539. Este teste existe para que a correção acima não seja "resolvida"
    /// simplesmente removendo a renumeração.
    /// </summary>
    [Fact]
    public async Task EmissaoQuePassaNaValidacao_AvancaNumeracaoAntesDeTransmitir()
    {
        var nota = NotaRejeitadaComCstIncompativel();
        // Corrige o que a validação barrava — é o que o operador faz em Estoque.
        nota.Itens.First().CstIcms = null;
        nota.Itens.First().CsosnIcms = "102";

        var (servico, repositorio, api) = CriarServico(nota);

        string? numeroTransmitido = null;
        api.Setup(a => a.EmitirAsync(
                It.IsAny<NotaFiscal>(), It.IsAny<EmitenteFiscalDto>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((NotaFiscal n, EmitenteFiscalDto _, string _, string? _, string? _, string? _, CancellationToken _)
                => numeroTransmitido = n.Numero)
            .ReturnsAsync(new ResultadoEmissaoFiscalDto { Aprovado = true, CStat = "100", XMotivo = "Autorizado o uso da NF-e" });

        await servico.EmitirAsync(nota.Id);

        Assert.Equal("18", numeroTransmitido);
        // Gravado antes do POST: o número transmitido e o número persistido não podem divergir.
        repositorio.Verify(r => r.AtualizarAsync(
            It.Is<NotaFiscal>(n => n.Numero == "18"), false, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
