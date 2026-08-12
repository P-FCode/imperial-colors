using System.Text.Json.Serialization;

namespace ImperialColors.Infrastructure.Fiscal.Contracts;

/// <summary>
/// <c>IBSCBSTot</c> é grupo IRMÃO de <c>ICMSTot</c> dentro de <c>total</c>, nunca aninhado
/// dentro dele — colocá-lo dentro de <c>ICMSTot</c> quebra a validação XSD (seção 4.10 do
/// guia, erro fácil de cometer "porque faz sentido à primeira vista"). Todos os campos
/// monetários são <c>decimal</c> (número JSON) — ver nota em <see cref="ProdContract"/> sobre
/// por que <c>string</c> quebra a desserialização do lado da API.
/// </summary>
public class TotalContract
{
    [JsonPropertyName("ICMSTot")] public IcmsTotContract ICMSTot { get; set; } = new();
    [JsonPropertyName("IBSCBSTot")] public IbsCbsTotContract IBSCBSTot { get; set; } = new();
}

public class IcmsTotContract
{
    [JsonPropertyName("vBC")] public decimal VBC { get; set; }
    [JsonPropertyName("vICMS")] public decimal VICMS { get; set; }
    [JsonPropertyName("vICMSDeson")] public decimal VICMSDeson { get; set; }
    [JsonPropertyName("vFCP")] public decimal VFCP { get; set; }
    [JsonPropertyName("vBCST")] public decimal VBCST { get; set; }
    [JsonPropertyName("vST")] public decimal VST { get; set; }
    [JsonPropertyName("vFCPST")] public decimal VFCPST { get; set; }
    [JsonPropertyName("vFCPSTRet")] public decimal VFCPSTRet { get; set; }
    [JsonPropertyName("vProd")] public decimal VProd { get; set; }
    [JsonPropertyName("vFrete")] public decimal VFrete { get; set; }
    [JsonPropertyName("vSeg")] public decimal VSeg { get; set; }
    [JsonPropertyName("vDesc")] public decimal VDesc { get; set; }
    [JsonPropertyName("vII")] public decimal VII { get; set; }
    [JsonPropertyName("vIPI")] public decimal VIPI { get; set; }
    [JsonPropertyName("vIPIDevol")] public decimal VIPIDevol { get; set; }
    [JsonPropertyName("vPIS")] public decimal VPIS { get; set; }
    [JsonPropertyName("vCOFINS")] public decimal VCOFINS { get; set; }
    [JsonPropertyName("vOutro")] public decimal VOutro { get; set; }
    [JsonPropertyName("vNF")] public decimal VNF { get; set; }
    [JsonPropertyName("vTotTrib")] public decimal? VTotTrib { get; set; }
}

public class IbsCbsTotContract
{
    [JsonPropertyName("vBCIBSCBS")] public decimal VBCIBSCBS { get; set; }
    [JsonPropertyName("gIBS")] public GIbsTotContract GIBS { get; set; } = new();
    [JsonPropertyName("gCBS")] public GCbsTotContract GCBS { get; set; } = new();
}

public class GIbsTotContract
{
    [JsonPropertyName("gIBSUF")] public GDifDevTribUfContract GIBSUF { get; set; } = new();
    [JsonPropertyName("gIBSMun")] public GDifDevTribMunContract GIBSMun { get; set; } = new();
    [JsonPropertyName("vIBS")] public decimal VIBS { get; set; }
    [JsonPropertyName("vCredPres")] public decimal VCredPres { get; set; }
    [JsonPropertyName("vCredPresCondSus")] public decimal VCredPresCondSus { get; set; }
}

public class GDifDevTribUfContract
{
    [JsonPropertyName("vDif")] public decimal VDif { get; set; }
    [JsonPropertyName("vDevTrib")] public decimal VDevTrib { get; set; }
    [JsonPropertyName("vIBSUF")] public decimal VIBSUF { get; set; }
}

public class GDifDevTribMunContract
{
    [JsonPropertyName("vDif")] public decimal VDif { get; set; }
    [JsonPropertyName("vDevTrib")] public decimal VDevTrib { get; set; }
    [JsonPropertyName("vIBSMun")] public decimal VIBSMun { get; set; }
}

public class GCbsTotContract
{
    [JsonPropertyName("vDif")] public decimal VDif { get; set; }
    [JsonPropertyName("vDevTrib")] public decimal VDevTrib { get; set; }
    [JsonPropertyName("vCBS")] public decimal VCBS { get; set; }
    [JsonPropertyName("vCredPres")] public decimal VCredPres { get; set; }
    [JsonPropertyName("vCredPresCondSus")] public decimal VCredPresCondSus { get; set; }
}

public class TranspContract
{
    /// <summary>Código oficial de <c>modFrete</c> (0-4, 9) — mesmo valor numérico do
    /// enum <c>ModalidadeFrete</c> do domínio. Continua <c>string</c>: <c>TTransp.ModFrete</c>
    /// no modelo real da API também é <c>string</c>.</summary>
    [JsonPropertyName("modFrete")] public string ModFrete { get; set; } = "9";
}

public class PagContract
{
    [JsonPropertyName("detPag")] public List<DetPagContract> DetPag { get; set; } = new();
}

public class DetPagContract
{
    [JsonPropertyName("indPag")] public string? IndPag { get; set; }

    /// <summary>"01"=Dinheiro, "03"=Crédito, "04"=Débito, "15"=Boleto, "17"=Pix, "90"=Sem
    /// Pagamento, "99"=Outros.</summary>
    [JsonPropertyName("tPag")] public string TPag { get; set; } = "01";

    /// <summary>Nullable de propósito — "Esta tag poderá ser omitida quando a tag tPag=90
    /// (Sem Pagamento), caso contrário deverá ser preenchida" (leiauteNFe_v4.00.xsd,
    /// elemento vPag). Para qualquer outro tPag, sempre populado pelo builder.</summary>
    [JsonPropertyName("vPag")] public decimal? VPag { get; set; }
}
