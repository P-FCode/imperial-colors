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
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: "00", csosnIcms: null, aliquotaIcms: 18m, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: "00", csosnIcms: null, aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: cst, csosnIcms: null, aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(0m, resultado.VIcms);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("ICMS"));
    }

    // ===== ICMS-ST "para frente" (CST 10 / CSOSN 201/202/203) =====

    [Theory]
    [InlineData("10")]
    public void CalcularItem_Cst10SemMvaOuAliquotaSt_GeraAvisoENaoCalcula(string cst)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: cst, csosnIcms: null, aliquotaIcms: 18m, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Null(resultado.VBcIcmsSt);
        Assert.Equal(0m, resultado.VIcmsSt);
        Assert.Contains(resultado.Avisos, a => a.Contains("Substituição Tributária") && a.Contains("MVA"));
    }

    /// <summary>Regressão: CST 10 tem grupo próprio (igual CST 00) MAIS o grupo ST — o item
    /// 100,00 com ICMS próprio 18% e MVA 40%/ICMS-ST 18% deve calcular vICMS=18,00,
    /// vBCST=140,00 (100 × 1,40), vICMSST bruto=25,20, e vICMSST líquido = 25,20 − 18,00 = 7,20
    /// (dedução do ICMS próprio, como manda a seção 4.7 do guia).</summary>
    [Fact]
    public void CalcularItem_Cst10ComMvaEAliquotaSt_CalculaGrupoProprioEStCorretamente()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: "10", csosnIcms: null, aliquotaIcms: 18m, reducaoBaseCalculo: null,
            mva: 40m, aliquotaIcmsSt: 18m, aliquotaIcmsStRetido: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(100m, resultado.VBcIcms);
        Assert.Equal(18m, resultado.VIcms);
        Assert.Equal(140m, resultado.VBcIcmsSt);
        Assert.Equal(7.20m, resultado.VIcmsSt);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("Substituição Tributária"));
    }

    [Theory]
    [InlineData("201")]
    [InlineData("202")]
    [InlineData("203")]
    public void CalcularItem_CsosnStParaFrenteSemMvaOuAliquotaSt_GeraAvisoENaoCalcula(string csosn)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: csosn, aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Null(resultado.VBcIcmsSt);
        Assert.Equal(0m, resultado.VIcmsSt);
        Assert.Contains(resultado.Avisos, a => a.Contains("Substituição Tributária") && a.Contains("MVA"));
    }

    /// <summary>CSOSN não destaca ICMS próprio (Simples Nacional) — diferente do CST 10, o
    /// ICMS-ST aqui NÃO deduz nenhum "vICMS próprio": vICMSST = vBCST × pICMSST direto.</summary>
    [Theory]
    [InlineData("201")]
    [InlineData("202")]
    [InlineData("203")]
    public void CalcularItem_CsosnStParaFrenteComMvaEAliquotaSt_CalculaSemDeduzirIcmsProprio(string csosn)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: csosn, aliquotaIcms: null, reducaoBaseCalculo: null,
            mva: 40m, aliquotaIcmsSt: 18m, aliquotaIcmsStRetido: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(0m, resultado.VIcms); // CSOSN nunca destaca ICMS próprio
        Assert.Equal(140m, resultado.VBcIcmsSt);
        Assert.Equal(25.20m, resultado.VIcmsSt); // 140 × 18%, sem dedução
    }

    // ===== ICMS-ST retido anteriormente (CST 60 / CSOSN 500) =====

    /// <summary>Regressão real: nota rejeitada pela SEFAZ com "Nao informada vBCSTRet, pST e
    /// vICMSSTRet" — CSOSN 500 passava sem nenhum aviso, e CST 60 gerava um aviso genérico de
    /// "não implementado" em vez de indicar o campo específico que falta.</summary>
    [Theory]
    [InlineData("60")]
    public void CalcularItem_Cst60SemAliquotaStRetido_GeraAvisoENaoCalcula(string cst)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: cst, csosnIcms: null, aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Null(resultado.VBcIcmsStRetido);
        Assert.Equal(0m, resultado.VIcmsStRetido);
        Assert.Contains(resultado.Avisos, a => a.Contains("pST"));
    }

    [Fact]
    public void CalcularItem_Cst60ComAliquotaStRetido_CalculaVBcStRetEVIcmsStRet()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: "60", csosnIcms: null, aliquotaIcms: null, reducaoBaseCalculo: null,
            mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: 12m,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(100m, resultado.VBcIcmsStRetido);
        Assert.Equal(12m, resultado.VIcmsStRetido); // 100 × 12%
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("Substituição Tributária") || a.Contains("pST"));
    }

    [Theory]
    [InlineData("500")]
    public void CalcularItem_Csosn500SemAliquotaStRetido_GeraAvisoENaoCalcula(string csosn)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: csosn, aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Null(resultado.VBcIcmsStRetido);
        Assert.Equal(0m, resultado.VIcmsStRetido);
        Assert.Contains(resultado.Avisos, a => a.Contains("pST"));
    }

    [Fact]
    public void CalcularItem_Csosn500ComAliquotaStRetido_CalculaVBcStRetEVIcmsStRet()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: "500", aliquotaIcms: null, reducaoBaseCalculo: null,
            mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: 12m,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(100m, resultado.VBcIcmsStRetido);
        Assert.Equal(12m, resultado.VIcmsStRetido);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("Substituição Tributária") || a.Contains("pST"));
    }

    [Fact]
    public void CalcularItem_Csosn900Outros_GeraAvisoDeRevisaoManual()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: "900", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(0m, resultado.VIcms);
        Assert.Contains(resultado.Avisos, a => a.Contains("CSOSN '900'"));
    }

    [Theory]
    [InlineData("101")]
    [InlineData("102")]
    [InlineData("103")]
    [InlineData("300")]
    [InlineData("400")]
    public void CalcularItem_CsosnSemDestaque_NaoCalculaSemAviso(string csosn)
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: csosn, aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
            cstPis: null, aliquotaPis: null,
            cstCofins: null, aliquotaCofins: null,
            cstIpi: null, aliquotaIpi: null,
            0, 0, 0);

        Assert.Equal(0m, resultado.VIcms);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("ICMS"));
    }

    [Fact]
    public void CalcularItem_SemCstOuCsosnIcms_GeraAviso()
    {
        var resultado = CalculoFiscalHelper.CalcularItem(
            1, "Produto", 100m,
            cstIcms: null, csosnIcms: null, aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
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
            cstIcms: null, csosnIcms: "102", aliquotaIcms: null, reducaoBaseCalculo: null, mva: null, aliquotaIcmsSt: null, aliquotaIcmsStRetido: null,
            cstPis: "07", aliquotaPis: null,
            cstCofins: "07", aliquotaCofins: null,
            cstIpi: cst, aliquotaIpi: null,
            0, 0, 0);

        Assert.Null(resultado.VIpi);
        Assert.DoesNotContain(resultado.Avisos, a => a.Contains("IPI"));
    }
}
