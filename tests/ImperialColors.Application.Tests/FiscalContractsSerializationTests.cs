using System.Text.Json;
using ImperialColors.Infrastructure.Fiscal;
using ImperialColors.Infrastructure.Fiscal.Contracts;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Garante que o payload serializado bate byte-a-byte com os nomes de campo exigidos pela
/// API Fiscal (GUIA_INTEGRACAO.md seções 4.2/4.10/8) — a convenção de maiúsculas/minúsculas
/// desses campos não é PascalCase nem camelCase puro ("cUF" vs "CNPJ" vs "CFOP" no mesmo
/// objeto), então um erro de digitação num <c>[JsonPropertyName]</c> nunca aparece como erro
/// de compilação, só como uma emissão real falhando contra a API — daí o valor deste teste.
/// </summary>
public class FiscalContractsSerializationTests
{
    private static string Serializar<T>(T valor) => JsonSerializer.Serialize(valor, FiscalJsonOptions.Padrao);

    [Fact]
    public void Ide_SerializaComNomesExatosDoGuia()
    {
        var json = Serializar(new IdeContract
        {
            CUF = "41",
            NatOp = "VENDA DE MERCADORIA",
            Mod = "55",
            NNF = "55001",
            CMunFG = "4106902",
            CNF = "12345678"
        });

        Assert.Contains("\"cUF\":\"41\"", json);
        Assert.Contains("\"natOp\":\"VENDA DE MERCADORIA\"", json);
        Assert.Contains("\"nNF\":\"55001\"", json);
        Assert.Contains("\"cMunFG\":\"4106902\"", json);
        Assert.Contains("\"cNF\":\"12345678\"", json);
        Assert.Contains("\"tpAmb\":", json);
        Assert.Contains("\"finNFe\":", json);
        Assert.Contains("\"indPres\":", json);

        // Nenhum vestígio de PascalCase deveria sobreviver à serialização.
        Assert.DoesNotContain("\"CUF\":", json);
        Assert.DoesNotContain("\"NatOp\":", json);
        Assert.DoesNotContain("\"NNF\":", json);
        Assert.DoesNotContain("\"TpAmb\":", json);
    }

    [Fact]
    public void Emit_SerializaCamposEmMaiusculoOndeAApiExige()
    {
        var json = Serializar(new EmitContract { CNPJ = "13416624000136", IE = "1234567891", CRT = "1" });

        Assert.Contains("\"CNPJ\":\"13416624000136\"", json);
        Assert.Contains("\"IE\":\"1234567891\"", json);
        Assert.Contains("\"CRT\":\"1\"", json);
        Assert.Contains("\"xNome\":", json);
        Assert.Contains("\"enderEmit\":", json);
    }

    [Fact]
    public void Endereco_UfECepSaoTotalmenteMaiusculas()
    {
        var json = Serializar(new EnderecoContract { UF = "PR", CEP = "80530000" });

        Assert.Contains("\"UF\":\"PR\"", json);
        Assert.Contains("\"CEP\":\"80530000\"", json);
        Assert.Contains("\"xLgr\":", json);
        Assert.Contains("\"cMun\":", json);
    }

    [Fact]
    public void ProdContract_SerializaNcmCfopEmMaiusculoEDemaisCamposEmCamelCase()
    {
        var json = Serializar(new ProdContract { NCM = "84713012", CFOP = "5102", CProd = "P001" });

        Assert.Contains("\"NCM\":\"84713012\"", json);
        Assert.Contains("\"CFOP\":\"5102\"", json);
        Assert.Contains("\"cProd\":\"P001\"", json);
        Assert.Contains("\"vUnCom\":", json);
        Assert.Contains("\"indTot\":", json);
    }

    /// <summary>
    /// Regressão do bug real por trás do HTTP 500 "erro interno" persistente: o modelo que a
    /// API de fato desserializa (<c>Fiscal.Shared.Models.TProd</c>/<c>TICMSTot</c>/<c>TDetPag</c>
    /// etc., conferido em PFCode\API-NF\src\Fiscal.Shared\Models) declara os campos monetários e
    /// de quantidade como <c>decimal</c> puro — sem <c>JsonNumberHandling.AllowReadingFromString</c>
    /// configurado em nenhum dos Program.cs das APIs. Enviar esses campos como string quoted
    /// (ex.: <c>"vProd": "100.00"</c>, formato do exemplo da seção 4.2 do guia, que está incorreto/
    /// desatualizado) quebra a desserialização do <c>[FromBody]</c> — o exemplo de NFC-e da seção
    /// 5.3 do guia mostra corretamente os mesmos campos como número puro, batendo com o código-fonte
    /// real. Este teste trava que os campos numéricos saem SEM aspas.
    /// </summary>
    [Fact]
    public void ProdContract_CamposNumericosSerializamComoNumeroJsonSemAspas()
    {
        var json = Serializar(new ProdContract
        {
            NCM = "84713012",
            CFOP = "5102",
            CProd = "P001",
            QCom = 1m,
            VUnCom = 100m,
            VProd = 100m,
            QTrib = 1m,
            VUnTrib = 100m
        });

        Assert.Contains("\"qCom\":1", json);
        Assert.Contains("\"vUnCom\":100", json);
        Assert.Contains("\"vProd\":100", json);
        Assert.Contains("\"qTrib\":1", json);
        Assert.Contains("\"vUnTrib\":100", json);

        // Nenhum desses campos pode sair entre aspas — isso quebra a desserialização do lado da API.
        Assert.DoesNotContain("\"qCom\":\"", json);
        Assert.DoesNotContain("\"vUnCom\":\"", json);
        Assert.DoesNotContain("\"vProd\":\"", json);
    }

    [Fact]
    public void TotalETribDetails_CamposNumericosSerializamComoNumeroJsonSemAspas()
    {
        var total = new TotalContract();
        total.ICMSTot.VBC = 100m;
        total.ICMSTot.VNF = 118m;
        total.IBSCBSTot.VBCIBSCBS = 100m;
        var jsonTotal = Serializar(total);

        Assert.Contains("\"vBC\":100", jsonTotal);
        Assert.Contains("\"vNF\":118", jsonTotal);
        Assert.Contains("\"vBCIBSCBS\":100", jsonTotal);
        Assert.DoesNotContain("\"vNF\":\"", jsonTotal);

        var pag = new DetPagContract { TPag = "01", VPag = 118m };
        var jsonPag = Serializar(pag);
        Assert.Contains("\"vPag\":118", jsonPag);
        Assert.DoesNotContain("\"vPag\":\"", jsonPag);

        var trib = new TribDetailsContract { VBC = 100m, VIBS = 0.10m };
        var jsonTrib = Serializar(trib);
        Assert.Contains("\"vBC\":100", jsonTrib);
        Assert.Contains("\"vIBS\":0.1", jsonTrib);
        Assert.DoesNotContain("\"vIBS\":\"", jsonTrib);
    }

    [Fact]
    public void Imposto_GruposDeNivelSuperiorFicamTodosEmMaiusculo()
    {
        var json = Serializar(new ImpostoContract());

        Assert.Contains("\"ICMS\":", json);
        Assert.Contains("\"PIS\":", json);
        Assert.Contains("\"COFINS\":", json);
        Assert.Contains("\"IBSCBS\":", json);
        Assert.Contains("\"ICMSDetails\":", json);
        Assert.Contains("\"PISDetails\":", json);
        Assert.Contains("\"COFINSDetails\":", json);
        Assert.Contains("\"tribDetails\":", json);
    }

    [Fact]
    public void IbsCbsTot_FicaSeparadoDeIcmsTotComoGrupoIrmao()
    {
        var json = Serializar(new TotalContract());

        Assert.Contains("\"ICMSTot\":", json);
        Assert.Contains("\"IBSCBSTot\":", json);
        Assert.Contains("\"gIBS\":", json);
        Assert.Contains("\"gCBS\":", json);
        Assert.Contains("\"vBCIBSCBS\":", json);
    }

    [Fact]
    public void CancelamentoRequest_SerializaTudoEmCamelCasePuro()
    {
        var json = Serializar(new CancelamentoRequest
        {
            ChaveAcesso = "412608...",
            Cnpj = "13416624000136",
            NProt = "141260000123456",
            Justificativa = "Cancelamento por erro de digitacao no pedido do cliente"
        });

        Assert.Contains("\"chaveAcesso\":", json);
        Assert.Contains("\"cnpj\":", json);
        Assert.Contains("\"nProt\":", json);
        Assert.Contains("\"justificativa\":", json);
        Assert.DoesNotContain("\"ChaveAcesso\":", json);
    }

    [Fact]
    public void InutilizacaoRequest_CUFSerializaComOEFMaiusculos()
    {
        var json = Serializar(new InutilizacaoRequest { CUF = "41" });

        Assert.Contains("\"cUF\":\"41\"", json);
        Assert.DoesNotContain("\"cuf\":", json);
        Assert.DoesNotContain("\"CUF\":", json);
    }

    /// <summary>
    /// Regressão do erro real reportado pelo operador ao clicar "Consultar Status": a API
    /// Fiscal já foi observada devolvendo campos documentados como string
    /// (GUIA_INTEGRACAO.md seção 7.1, ex. <c>"cStat": "100"</c>) como número JSON puro
    /// (<c>"cStat": 100</c>) — o <see cref="System.Text.Json.JsonSerializer"/> padrão
    /// lançava "Cannot get the value of a token type 'Number' as a string.", que a UI
    /// mostrava (incorretamente) como "Erro real do banco". Ver <see cref="StringOrNumberConverter"/>.
    /// </summary>
    [Fact]
    public void StatusNotaResponse_DesserializaMesmoQuandoCamposDeStringVemComoNumeroJson()
    {
        const string json = """
            {
              "chaveAcesso": "41260813416624000136550010000000011123456785",
              "tpAmb": 2,
              "cStat": 100,
              "xMotivo": "Autorizado o uso da NF-e",
              "nProt": 141260000123456,
              "situacao": "Autorizada",
              "confirmadoNaSefaz": true,
              "eventosRegistrados": [
                { "tpEvento": "110111", "cStat": 135, "nSeqEvento": 1 }
              ]
            }
            """;

        var resultado = JsonSerializer.Deserialize<StatusNotaResponse>(json, FiscalJsonOptions.Padrao);

        Assert.NotNull(resultado);
        Assert.Equal("2", resultado!.TpAmb);
        Assert.Equal("100", resultado.CStat);
        Assert.Equal("141260000123456", resultado.NProt);
        Assert.NotNull(resultado.EventosRegistrados);
        Assert.Equal("135", resultado.EventosRegistrados![0].CStat);
        Assert.Equal("1", resultado.EventosRegistrados[0].NSeqEvento);
    }
}
