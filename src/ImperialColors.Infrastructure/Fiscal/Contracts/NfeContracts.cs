using System.Text.Json.Serialization;

namespace ImperialColors.Infrastructure.Fiscal.Contracts;

/// <summary>
/// Contratos JSON do payload de emissão de NF-e/NFC-e (<c>POST /api/v1/nfe/emitir</c> e
/// <c>/nfce/emitir</c>), espelhando *exatamente* as seções 4 e 5 do GUIA_INTEGRACAO.md da
/// API Fiscal PFCode. Toda propriedade tem <see cref="JsonPropertyNameAttribute"/> com o
/// nome exato usado no JSON de exemplo do guia — a convenção de maiúsculas/minúsculas
/// desses campos não é PascalCase nem camelCase puro (é derivada das tags do XML da NF-e:
/// "cUF", "xNome", "CNPJ", "CFOP" convivem no mesmo objeto), então nenhuma
/// <c>PropertyNamingPolicy</c> automática dá conta — cada campo precisa do nome literal.
/// </summary>
public class EmissaoNotaRequest
{
    [JsonPropertyName("infNFe")] public InfNFeContract InfNFe { get; set; } = new();
}

public class InfNFeContract
{
    [JsonPropertyName("versao")] public string Versao { get; set; } = "4.00";
    [JsonPropertyName("ide")] public IdeContract Ide { get; set; } = new();
    [JsonPropertyName("emit")] public EmitContract Emit { get; set; } = new();
    [JsonPropertyName("dest")] public DestContract? Dest { get; set; }
    [JsonPropertyName("det")] public List<DetContract> Det { get; set; } = new();
    [JsonPropertyName("total")] public TotalContract Total { get; set; } = new();
    [JsonPropertyName("transp")] public TranspContract Transp { get; set; } = new();
    [JsonPropertyName("pag")] public PagContract Pag { get; set; } = new();
}

public class IdeContract
{
    [JsonPropertyName("cUF")] public string? CUF { get; set; }
    [JsonPropertyName("natOp")] public string NatOp { get; set; } = string.Empty;

    /// <summary>Sempre "55" (NF-e) ou "65" (NFC-e).</summary>
    [JsonPropertyName("mod")] public string Mod { get; set; } = "55";
    [JsonPropertyName("serie")] public string Serie { get; set; } = string.Empty;
    [JsonPropertyName("nNF")] public string NNF { get; set; } = string.Empty;

    /// <summary>ISO-8601 com offset (ex.: 2026-08-05T08:00:00-03:00) — nunca UTC/"Z".</summary>
    [JsonPropertyName("dhEmi")] public string DhEmi { get; set; } = string.Empty;

    /// <summary>"0"=Entrada, "1"=Saída.</summary>
    [JsonPropertyName("tpNF")] public string TpNF { get; set; } = "1";

    /// <summary>"1"=Interna, "2"=Interestadual, "3"=Exterior.</summary>
    [JsonPropertyName("idDest")] public string IdDest { get; set; } = "1";
    [JsonPropertyName("cMunFG")] public string CMunFG { get; set; } = string.Empty;

    /// <summary>"1"=Retrato (NF-e), "4"=NFC-e (bobina térmica).</summary>
    [JsonPropertyName("tpImp")] public string TpImp { get; set; } = "1";

    /// <summary>"1"=normal (padrão). Contingência SVC ("6"/"7") não é usada por este
    /// módulo — fica documentada como pendência conhecida do plano.</summary>
    [JsonPropertyName("tpEmis")] public string TpEmis { get; set; } = "1";
    [JsonPropertyName("dhCont")] public string? DhCont { get; set; }
    [JsonPropertyName("xJust")] public string? XJust { get; set; }

    /// <summary>"1"=Produção, "2"=Homologação.</summary>
    [JsonPropertyName("tpAmb")] public string TpAmb { get; set; } = "2";

    /// <summary>"1"=Normal, "2"=Complementar, "3"=Ajuste, "4"=Devolução.</summary>
    [JsonPropertyName("finNFe")] public string FinNFe { get; set; } = "1";

    /// <summary>"0"=Não é consumidor final, "1"=Consumidor final.</summary>
    [JsonPropertyName("indFinal")] public string IndFinal { get; set; } = "0";

    /// <summary>"0".."9" — presença do comprador.</summary>
    [JsonPropertyName("indPres")] public string IndPres { get; set; } = "1";
    [JsonPropertyName("procEmi")] public string ProcEmi { get; set; } = "0";
    [JsonPropertyName("verProc")] public string VerProc { get; set; } = "ImperialColors/1.0";
    [JsonPropertyName("cNF")] public string? CNF { get; set; }
}

public class EmitContract
{
    [JsonPropertyName("CNPJ")] public string CNPJ { get; set; } = string.Empty;
    [JsonPropertyName("xNome")] public string XNome { get; set; } = string.Empty;
    [JsonPropertyName("xFant")] public string? XFant { get; set; }
    [JsonPropertyName("enderEmit")] public EnderecoContract EnderEmit { get; set; } = new();
    [JsonPropertyName("IE")] public string IE { get; set; } = string.Empty;

    /// <summary>"1"=Simples Nacional, "2"=Simples excesso sublimite, "3"=Regime Normal, "4"=MEI.</summary>
    [JsonPropertyName("CRT")] public string CRT { get; set; } = "1";
}

public class EnderecoContract
{
    [JsonPropertyName("xLgr")] public string XLgr { get; set; } = string.Empty;
    [JsonPropertyName("nro")] public string Nro { get; set; } = string.Empty;
    [JsonPropertyName("xCpl")] public string? XCpl { get; set; }
    [JsonPropertyName("xBairro")] public string XBairro { get; set; } = string.Empty;
    [JsonPropertyName("cMun")] public string CMun { get; set; } = string.Empty;
    [JsonPropertyName("xMun")] public string XMun { get; set; } = string.Empty;
    [JsonPropertyName("UF")] public string UF { get; set; } = string.Empty;
    [JsonPropertyName("CEP")] public string CEP { get; set; } = string.Empty;
    [JsonPropertyName("cPais")] public string? CPais { get; set; } = "1058";
    [JsonPropertyName("xPais")] public string? XPais { get; set; } = "BRASIL";
    [JsonPropertyName("fone")] public string? Fone { get; set; }
}

public class DestContract
{
    [JsonPropertyName("CNPJ")] public string? CNPJ { get; set; }
    [JsonPropertyName("CPF")] public string? CPF { get; set; }
    [JsonPropertyName("xNome")] public string XNome { get; set; } = string.Empty;
    [JsonPropertyName("enderDest")] public EnderecoContract? EnderDest { get; set; }

    /// <summary>"1"=Contribuinte ICMS, "2"=Isento, "9"=Não contribuinte.</summary>
    [JsonPropertyName("indIEDest")] public string IndIEDest { get; set; } = "9";
    [JsonPropertyName("IE")] public string? IE { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
}
