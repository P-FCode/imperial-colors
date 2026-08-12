using ImperialColors.Application.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class CalculoFiscalHelperTests
{
    // Alíquotas do ano-piloto 2026 usadas nos exemplos do próprio guia de integração:
    // IBS = 0,10% (tudo alocado à UF) + CBS = 0,90%.
    private const decimal AliqIbsUf = 0.10m;
    private const decimal AliqIbsMunicipio = 0.00m;
    private const decimal AliqCbs = 0.90m;

    [Fact]
    public void CalcularItem_Csosn102_NaoDestacaIcmsNaNota()
    {
        // CSOSN 102 é o caso mais comum de loja no Simples Nacional — o ICMS já está
        // embutido no preço, a nota não separa vBC/vICMS.
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Tinta Coral", 100m,
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null,
            cstPis: "07", aliquotaPis: null,
            cstCofins: "07", aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            AliqIbsUf, AliqIbsMunicipio, AliqCbs);

        Assert.Equal(0m, resultado.VIcms);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("ICMS"));
    }

    [Fact]
    public void CalcularItem_CstIcms00ComAliquota_CalculaIcmsCorretamente()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Tinta Coral", 100m,
            cstIcms: "00", csosnIcms: null, aliquotaIcms: 18m,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(18m, resultado.VIcms);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("ICMS"));
    }

    [Fact]
    public void CalcularItem_CstIcms00SemAliquotaCadastrada_GeraAvisoENaoCalcula()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Tinta Coral", 100m,
            cstIcms: "00", csosnIcms: null, aliquotaIcms: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(0m, resultado.VIcms);
        Assert.Contains(resultado.Avisos, a => a.Contains("alíquota"));
    }

    [Theory]
    [InlineData("40")]
    [InlineData("41")]
    [InlineData("50")]
    public void CalcularItem_CstIcmsIsentoOuSuspenso_NaoCalculaSemAviso(string cst)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: cst, csosnIcms: null, aliquotaIcms: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(0m, resultado.VIcms);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("ICMS"));
    }

    [Theory]
    [InlineData("10")]
    [InlineData("60")]
    public void CalcularItem_CstIcmsSubstituicaoTributaria_GeraAvisoDeRevisaoManual(string cst)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: cst, csosnIcms: null, aliquotaIcms: 18m,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(0m, resultado.VIcms);
        Assert.Contains(resultado.Avisos, a => a.Contains("Substituição Tributária"));
    }

    [Fact]
    public void CalcularItem_SemCstOuCsosnIcms_GeraAviso()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: null, aliquotaIcms: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Contains(resultado.Avisos, a => a.Contains("CST/CSOSN"));
    }

    [Fact]
    public void CalcularItem_PisCofinsCst01_CalculaCorretamente()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null,
            cstPis: "01", aliquotaPis: 1.65m,
            cstCofins: "01", aliquotaCofins: 7.60m,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(1.65m, resultado.VPis);
        Assert.Equal(7.60m, resultado.VCofins);
    }

    [Theory]
    [InlineData("04")]
    [InlineData("07")]
    [InlineData("09")]
    public void CalcularItem_PisCofinsNaoTributado_NaoCalculaSemAviso(string cst)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null,
            cstPis: cst, aliquotaPis: null,
            cstCofins: cst, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(0m, resultado.VPis);
        Assert.Equal(0m, resultado.VCofins);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("PIS") || a.Contains("COFINS"));
    }

    [Fact]
    public void CalcularItem_IbsCbs_CalculaComAliquotasPadraoDaEmpresa()
    {
        // Exemplo do próprio guia de integração: item de R$ 5,00, IBS=0,10%+CBS=0,90%
        // → vIBSUF ≈ 0,01 e vCBS ≈ 0,05 (arredondamento bate com o exemplo oficial).
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 5.00m,
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null,
            cstPis: "07", aliquotaPis: null,
            cstCofins: "07", aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            AliqIbsUf, AliqIbsMunicipio, AliqCbs);

        Assert.Equal(0.01m, resultado.VIbsUf);
        Assert.Equal(0.00m, resultado.VIbsMunicipio);
        Assert.Equal(0.05m, resultado.VCbs);
    }

    [Fact]
    public void CalcularItem_IbsCbsComAliquotaZero_NaoCalculaNada()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null,
            cstPis: "07", aliquotaPis: null,
            cstCofins: "07", aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(0m, resultado.VIbsUf);
        Assert.Equal(0m, resultado.VIbsMunicipio);
        Assert.Equal(0m, resultado.VCbs);
    }

    // ===== IPI =====

    [Fact]
    public void CalcularItem_SemCstIpi_NaoCalculaSemAviso()
    {
        // Diferente de ICMS/PIS/COFINS, IPI é opcional — a maioria dos produtos não tem CST
        // de IPI cadastrado, e isso não deveria gerar aviso de "pendência de cadastro".
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Tinta Coral", 100m,
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null,
            cstPis: "07", aliquotaPis: null,
            cstCofins: "07", aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Null(resultado.VIpi);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("IPI"));
    }

    [Fact]
    public void CalcularItem_CstIpi50ComAliquota_CalculaIpiCorretamente()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null,
            cstPis: "07", aliquotaPis: null,
            cstCofins: "07", aliquotaCofins: null,
            cstIpi: "50", aliquotaIpi: 5m,
            0, 0, 0);

        Assert.Equal(5m, resultado.VIpi);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("IPI"));
    }

    [Fact]
    public void CalcularItem_CstIpiTributadoSemAliquotaCadastrada_GeraAvisoENaoCalcula()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null,
            cstPis: "07", aliquotaPis: null,
            cstCofins: "07", aliquotaCofins: null,
            cstIpi: "50", aliquotaIpi: null,
            0, 0, 0);

        Assert.Null(resultado.VIpi);
        Assert.Contains(resultado.Avisos, a => a.Contains("IPI") && a.Contains("alíquota"));
    }

    [Theory]
    [InlineData("01")]
    [InlineData("53")]
    public void CalcularItem_CstIpiNaoTributado_NaoCalculaSemAviso(string cst)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null,
            cstPis: "07", aliquotaPis: null,
            cstCofins: "07", aliquotaCofins: null,
            cstIpi: cst, aliquotaIpi: null,
            0, 0, 0);

        Assert.Null(resultado.VIpi);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("IPI"));
    }
}
