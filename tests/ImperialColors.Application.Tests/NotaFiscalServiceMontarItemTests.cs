using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Regressão do XSD_VALIDATION "incomplete content... expected 'pICMS'": um produto com CST
/// de ICMS '00' (ou 20/51/90) cadastrado mas sem alíquota preenchida saía com
/// <c>ItemNotaFiscalDto.BaseIcms</c> nulo (a condição antiga era "só preenche vBC se a
/// alíquota estiver cadastrada"), então o grupo ICMS00 do XML ficava sem vBC/pICMS/vICMS —
/// campos obrigatórios no leiaute NFe — e a SEFAZ rejeitava a nota. <c>BaseIcms</c> agora
/// depende só do CST (o valor do item é sempre a base ad-valorem quando o CST exige o grupo),
/// não da alíquota estar cadastrada — quem bloqueia a emissão nesse cenário é
/// <see cref="Validation.NotaFiscalValidator"/> (ver NotaFiscalValidatorTests).
/// </summary>
public class NotaFiscalServiceMontarItemTests
{
    private const int ProdutoId = 1;
    private const decimal PrecoVenda = 100m;
    private const decimal Quantidade = 1m;

    private static NotaFiscalService CriarServico(TributacaoProduto? tributacao, ConfiguracaoFiscalEmpresaDto? empresa = null)
    {
        var produtoRepo = new Mock<IProdutoRepository>();
        produtoRepo.Setup(r => r.ObterPorIdAsync(ProdutoId)).ReturnsAsync(new Produto
        {
            Id = ProdutoId,
            CodigoInterno = "P001",
            Nome = "Galão de teste",
            Unidade = "UN",
            PrecoVenda = PrecoVenda
        });

        var tributacaoRepo = new Mock<ITributacaoProdutoRepository>();
        tributacaoRepo.Setup(r => r.ObterPorProdutoIdAsync(ProdutoId, It.IsAny<CancellationToken>())).ReturnsAsync(tributacao);

        var configuracaoFiscal = new Mock<IConfiguracaoFiscalService>();
        configuracaoFiscal.Setup(s => s.ObterConfiguracaoEmpresaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa ?? new ConfiguracaoFiscalEmpresaDto());

        return new NotaFiscalService(
            Mock.Of<INotaFiscalRepository>(),
            Mock.Of<IVendaRepository>(),
            Mock.Of<IClienteRepository>(),
            produtoRepo.Object,
            tributacaoRepo.Object,
            Mock.Of<ITributacaoCategoriaRepository>(),
            Mock.Of<IRepository<NaturezaOperacao>>(),
            configuracaoFiscal.Object,
            Mock.Of<IFiscalApiClient>());
    }

    [Theory]
    [InlineData("00")]
    [InlineData("20")]
    [InlineData("51")]
    [InlineData("90")]
    public async Task MontarItemAPartirDeProdutoAsync_CstTributacaoIntegralSemAliquotaCadastrada_PreencheBaseIcmsMesmoAssim(string cst)
    {
        var servico = CriarServico(new TributacaoProduto { ProdutoId = ProdutoId, CstIcms = cst, AliquotaIcms = null });

        var item = await servico.MontarItemAPartirDeProdutoAsync(ProdutoId, Quantidade, interestadual: false);

        // BaseIcms (vBC) tem que sair preenchido mesmo sem alíquota — é isso que garante que o
        // NotaFiscalPayloadBuilder tenta montar o grupo ICMS no XML; quem bloqueia a emissão por
        // faltar a alíquota é o NotaFiscalValidator, não um vBC silenciosamente omitido.
        Assert.Equal(PrecoVenda * Quantidade, item.BaseIcms);
        Assert.Null(item.AliquotaIcms);
        Assert.Contains(item.Avisos, a => a.Contains("exige alíquota"));
    }

    [Fact]
    public async Task MontarItemAPartirDeProdutoAsync_Cst00ComAliquotaCadastrada_PreencheBaseICMSEValorIcms()
    {
        var servico = CriarServico(new TributacaoProduto
        {
            ProdutoId = ProdutoId,
            CstIcms = "00",
            AliquotaIcms = 18m,
            CstPis = "07", // não tributado — evita aviso de "sem CST cadastrado" que poluiria a asserção de Avisos
            CstCofins = "07"
        });

        var item = await servico.MontarItemAPartirDeProdutoAsync(ProdutoId, Quantidade, interestadual: false);

        Assert.Equal(PrecoVenda * Quantidade, item.BaseIcms);
        Assert.Equal(18m, item.ValorIcms); // 100 * 18% = 18
        Assert.Empty(item.Avisos);
    }

    [Fact]
    public async Task MontarItemAPartirDeProdutoAsync_CsosnSimplesNacional_NaoPreencheBaseIcms()
    {
        // CSOSN nunca destaca vICMS na nota — imposto embutido no preço, sem grupo vBC/pICMS.
        var servico = CriarServico(new TributacaoProduto { ProdutoId = ProdutoId, CsosnIcms = "102" });

        var item = await servico.MontarItemAPartirDeProdutoAsync(ProdutoId, Quantidade, interestadual: false);

        Assert.Null(item.BaseIcms);
        Assert.Equal(0m, item.ValorIcms); // CalcularIcms nunca atribui VIcms pra CSOSN — fica no default (0), não null
    }

    [Fact]
    public async Task MontarItemAPartirDeProdutoAsync_SemTributacaoCadastrada_UsaCstIcmsPadraoDaEmpresaEPreencheBaseIcms()
    {
        var empresa = new ConfiguracaoFiscalEmpresaDto { CstIcmsPadrao = "00" };
        var servico = CriarServico(tributacao: null, empresa: empresa);

        var item = await servico.MontarItemAPartirDeProdutoAsync(ProdutoId, Quantidade, interestadual: false);

        Assert.Equal("00", item.CstIcms);
        Assert.Equal(PrecoVenda * Quantidade, item.BaseIcms);
    }
}
