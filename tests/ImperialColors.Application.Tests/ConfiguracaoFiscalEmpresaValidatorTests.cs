using ImperialColors.Application.DTOs;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using Xunit;

namespace ImperialColors.Application.Tests;

public class ConfiguracaoFiscalEmpresaValidatorTests
{
    private static ConfiguracaoFiscalEmpresaDto DtoVazio() => new();

    [Fact]
    public void Validar_DtoTotalmenteVazio_NaoLancaExcecao()
    {
        var ex = Record.Exception(() => ConfiguracaoFiscalEmpresaValidator.Validar(DtoVazio()));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("8001000")]   // 7 dígitos
    [InlineData("800010000")] // 9 dígitos
    [InlineData("8001000A")]  // não numérico
    public void Validar_CepComFormatoInvalido_LancaDomainException(string cep)
    {
        var dto = DtoVazio();
        dto.Cep = cep;

        var ex = Assert.Throws<DomainException>(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Contains("CEP", ex.Message);
    }

    [Fact]
    public void Validar_CepValido_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.Cep = "80010000";

        var ex = Record.Exception(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("410690")]   // 6 dígitos
    [InlineData("41069022")] // 8 dígitos
    public void Validar_CodigoIbgeComFormatoInvalido_LancaDomainException(string codigo)
    {
        var dto = DtoVazio();
        dto.CodigoMunicipioIbge = codigo;

        var ex = Assert.Throws<DomainException>(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Contains("IBGE", ex.Message);
    }

    [Fact]
    public void Validar_UfInvalida_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.Uf = "XX";

        var ex = Assert.Throws<DomainException>(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Contains("UF", ex.Message);
    }

    [Theory]
    [InlineData("PR")]
    [InlineData("sp")] // case-insensitive
    public void Validar_UfValida_NaoLancaExcecao(string uf)
    {
        var dto = DtoVazio();
        dto.Uf = uf;

        var ex = Record.Exception(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_SerieForaDaFaixa_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.Serie = "999"; // máximo é 889

        var ex = Assert.Throws<DomainException>(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Contains("Série", ex.Message);
    }

    [Fact]
    public void Validar_IdCscSemCsc_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.IdCscHomologacao = "000001"; // preencheu o id mas esqueceu o CSC

        var ex = Assert.Throws<DomainException>(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Contains("Homologação", ex.Message);
    }

    [Fact]
    public void Validar_CscEIdCscJuntos_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.IdCscProducao = "000001";
        dto.CscProducao = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        var ex = Record.Exception(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validar_AliquotaIbsForaDaFaixa_LancaDomainException(decimal aliquota)
    {
        var dto = DtoVazio();
        dto.AliquotaIbsUfPadrao = aliquota;

        var ex = Assert.Throws<DomainException>(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Contains("IBS", ex.Message);
    }

    [Fact]
    public void Validar_AliquotasDoAnoPiloto2026_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.AliquotaIbsUfPadrao = 0.10m;
        dto.AliquotaIbsMunicipioPadrao = 0.00m;
        dto.AliquotaCbsPadrao = 0.90m;

        var ex = Record.Exception(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Null(ex);
    }

    // ===== ICMS — Regra Geral (CST/CSOSN padrão da empresa) =====

    [Fact]
    public void Validar_CstECsosnIcmsPadraoPreenchidosJuntos_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CstIcmsPadrao = "00";
        dto.CsosnIcmsPadrao = "102";

        var ex = Assert.Throws<DomainException>(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Contains("mutuamente exclusivos", ex.Message);
    }

    [Fact]
    public void Validar_CstIcmsPadraoInvalido_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CstIcmsPadrao = "77"; // não existe na tabela oficial

        var ex = Assert.Throws<DomainException>(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Contains("CST de ICMS", ex.Message);
    }

    [Fact]
    public void Validar_CsosnIcmsPadraoInvalido_LancaDomainException()
    {
        var dto = DtoVazio();
        dto.CsosnIcmsPadrao = "999"; // não existe na tabela oficial

        var ex = Assert.Throws<DomainException>(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Contains("CSOSN", ex.Message);
    }

    [Theory]
    [InlineData("102")] // CSOSN — Simples Nacional
    [InlineData(null)]  // vazio — não obrigatório
    public void Validar_CsosnIcmsPadraoValido_NaoLancaExcecao(string? csosn)
    {
        var dto = DtoVazio();
        dto.CsosnIcmsPadrao = csosn;

        var ex = Record.Exception(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_CstIcmsPadraoValido_NaoLancaExcecao()
    {
        var dto = DtoVazio();
        dto.CstIcmsPadrao = "00"; // CST — Regime Normal

        var ex = Record.Exception(() => ConfiguracaoFiscalEmpresaValidator.Validar(dto));
        Assert.Null(ex);
    }
}
