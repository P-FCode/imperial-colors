using ImperialColors.Application.DTOs;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using Xunit;

namespace ImperialColors.Application.Tests;

public class TributacaoProdutoValidatorTests
{
    private static TributacaoProdutoDto DtoVazio() => new() { ProdutoId = 1 };

    [Fact]
    public void Validar_DtoTotalmenteVazio_NaoLancaExcecao()
    {
        // Produto ainda não teve a tributação preenchida — deve ser permitido.
        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(DtoVazio(), RegimeTributario.SimplesNacional));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData("1234567")]   // 7 dígitos
    [InlineData("123456789")] // 9 dígitos
    [InlineData("1234567A")]  // não numérico
    public void Validar_NcmComFormatoInvalido_LancaDomainException(string ncm)
    {
        var dto = DtoVazio();
        dto.Ncm = ncm;

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("NCM", ex.Message);
    }

    [Fact]
    public void Validar_NcmComOitoDigitos_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.Ncm = "32089000";

        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("123456")]  // 6 dígitos
    [InlineData("12345678")] // 8 dígitos
    public void Validar_CestComFormatoInvalido_LancaDomainException(string cest)
    {
        var dto = DtoVazio();
        dto.Cest = cest;

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("CEST", ex.Message);
    }

    [Fact]
    public void Validar_SimplesNacionalComCstPreenchido_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CstIcms = "00";

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("CSOSN", ex.Message);
    }

    [Fact]
    public void Validar_LucroPresumidoComCsosnPreenchido_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CsosnIcms = "101";

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.LucroPresumido));
        Assert.Contains("CST", ex.Message);
    }

    [Fact]
    public void Validar_CstECsosnPreenchidosJuntos_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CstIcms = "00";
        dto.CsosnIcms = "101";

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.LucroPresumido));
        Assert.Contains("mutuamente exclusivos", ex.Message);
    }

    [Fact]
    public void Validar_CsosnInvalido_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CsosnIcms = "999"; // não existe na tabela oficial

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("CSOSN", ex.Message);
    }

    [Fact]
    public void Validar_CsosnValido_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.CsosnIcms = "102";

        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_CstIcmsInvalido_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CstIcms = "99"; // não existe na tabela oficial de CST ICMS

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.LucroReal));
        Assert.Contains("CST de ICMS", ex.Message);
    }

    [Fact]
    public void Validar_CstIcmsValido_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.CstIcms = "00";

        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.LucroReal));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validar_AliquotaIcmsForaDaFaixa_LancaDomainException(decimal aliquota)
    {
        var dto = DtoVazio();
        dto.AliquotaIcms = aliquota;

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("ICMS", ex.Message);
    }

    [Fact]
    public void Validar_CstPisComFormatoInvalido_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CstPis = "1"; // precisa de 2 dígitos

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("PIS", ex.Message);
    }

    [Fact]
    public void Validar_CstIpiNaoPertenceATabelaOficial_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CstIpi = "77";

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("IPI", ex.Message);
    }

    [Theory]
    [InlineData("SEM GTIN")]
    [InlineData("7891234567895")] // 13 dígitos
    [InlineData("12345678")]      // 8 dígitos
    public void Validar_GtinTributavelValido_NaoLancaExcecao(string gtin)
    {
        var dto = DtoVazio();
        dto.GtinTributavel = gtin;

        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_GtinTributavelComTamanhoInvalido_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.GtinTributavel = "123"; // tamanho não corresponde a nenhum padrão EAN/GTIN

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("GTIN", ex.Message);
    }

    [Fact]
    public void Validar_FatorConversaoZeroOuNegativo_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.FatorConversao = 0;

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("conversão", ex.Message);
    }

    // --- Reforma Tributária (IBS/CBS/IS) ---

    [Fact]
    public void Validar_CstIbsCbsSozinhoSemCClassTrib_NaoLancaExcecao()
    {
        // CST sozinho (sem cClassTrib ainda) é permitido — o produto pode estar
        // em preenchimento gradual dos novos campos.
        var dto = DtoVazio();
        dto.CstIbsCbs = "200";

        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("20")]   // 2 dígitos
    [InlineData("2000")] // 4 dígitos
    [InlineData("2A0")]  // não numérico
    public void Validar_CstIbsCbsComFormatoInvalido_LancaDomainException(string cst)
    {
        var dto = DtoVazio();
        dto.CstIbsCbs = cst;

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("IBS/CBS", ex.Message);
    }

    [Theory]
    [InlineData("20001")]   // 5 dígitos
    [InlineData("2000133")] // 7 dígitos
    [InlineData("20001A")]  // não numérico
    public void Validar_CClassTribComFormatoInvalido_LancaDomainException(string cClassTrib)
    {
        var dto = DtoVazio();
        dto.CstIbsCbs = "200";
        dto.CClassTrib = cClassTrib;

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("cClassTrib", ex.Message);
    }

    [Fact]
    public void Validar_CClassTribSemCstIbsCbs_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CClassTrib = "200013"; // preencheu o cClassTrib mas esqueceu o CST

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("CST de IBS/CBS", ex.Message);
    }

    [Fact]
    public void Validar_CClassTribComPrefixoDiferenteDoCst_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CstIbsCbs = "200";
        dto.CClassTrib = "210013"; // prefixo "210" != CST "200"

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("inconsistente", ex.Message);
    }

    [Fact]
    public void Validar_CstECClassTribIbsCbsConsistentes_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.CstIbsCbs = "200";
        dto.CClassTrib = "200013"; // prefixo bate com o CST

        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_ImpostoSeletivoMesmaRegraDoIbsCbs_LancaDomainExceptionQuandoInconsistente()
    {
        var dto = DtoVazio();
        dto.CstIS = "800";
        dto.CClassTribIS = "810001"; // prefixo "810" != CST "800"

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.LucroReal));
        Assert.Contains("Imposto Seletivo", ex.Message);
    }

    [Fact]
    public void Validar_ProdutoComTributacaoLegadaEIbsCbsJuntos_NaoLancaExcecao()
    {
        // Cenário real de transição: produto mantém CSOSN (ICMS atual) e já recebe
        // o CST/cClassTrib novo em paralelo — as duas estruturas coexistem no XML
        // da NF-e durante o período de transição (2026-2033).
        var dto = DtoVazio();
        dto.Ncm = "32089000";
        dto.CsosnIcms = "102";
        dto.CstIbsCbs = "000";
        dto.CClassTrib = "000001";

        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Null(ex);
    }

    // --- MEI usa CSOSN, igual Simples Nacional (confirmado contra o CRT real da NF-e) ---

    [Fact]
    public void Validar_MeiComCstPreenchido_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CstIcms = "00";

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.Mei));
        Assert.Contains("CSOSN", ex.Message);
    }

    [Fact]
    public void Validar_MeiComCsosnValido_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.CsosnIcms = "102";

        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.Mei));
        Assert.Null(ex);
    }

    // --- CFOP ---

    [Theory]
    [InlineData("510")]   // 3 dígitos
    [InlineData("51022")] // 5 dígitos
    [InlineData("510A")]  // não numérico
    public void Validar_CfopDentroEstadoComFormatoInvalido_LancaDomainException(string cfop)
    {
        var dto = DtoVazio();
        dto.CfopDentroEstado = cfop;

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("CFOP", ex.Message);
    }

    [Fact]
    public void Validar_CfopDentroEstadoComPrefixoErrado_LancaDomainException()
    {
        // 6xxx é CFOP de venda para OUTRO estado — não serve para "dentro do estado".
        var dto = DtoVazio();
        dto.CfopDentroEstado = "6102";

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("dentro do estado", ex.Message);
    }

    [Fact]
    public void Validar_CfopForaEstadoComPrefixoErrado_LancaDomainException()
    {
        // 5xxx é CFOP de venda DENTRO do estado — não serve para "fora do estado".
        var dto = DtoVazio();
        dto.CfopForaEstado = "5102";

        var ex = Assert.Throws<DomainException>(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Contains("outro estado", ex.Message);
    }

    [Fact]
    public void Validar_CfopDentroEForaEstadoValidos_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.CfopDentroEstado = "5102";
        dto.CfopForaEstado = "6102";

        var ex = Record.Exception(() =>
            TributacaoProdutoValidator.Validar(dto, RegimeTributario.SimplesNacional));
        Assert.Null(ex);
    }
}
