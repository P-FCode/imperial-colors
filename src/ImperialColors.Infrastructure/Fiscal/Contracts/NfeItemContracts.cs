using System.Text.Json.Serialization;

namespace ImperialColors.Infrastructure.Fiscal.Contracts;

public class DetContract
{
    [JsonPropertyName("nItem")] public string NItem { get; set; } = string.Empty;
    [JsonPropertyName("prod")] public ProdContract Prod { get; set; } = new();
    [JsonPropertyName("imposto")] public ImpostoContract Imposto { get; set; } = new();
}

/// <summary>
/// Campos monetários/quantidade são <c>decimal</c> (número JSON), não <c>string</c> — o modelo
/// real que a API desserializa (<c>Fiscal.Shared.Models.TProd</c>) declara <c>QCom</c>/<c>VUnCom</c>/
/// <c>VProd</c>/<c>QTrib</c>/<c>VUnTrib</c> como <c>decimal</c> puro. Sem <c>JsonNumberHandling
/// .AllowReadingFromString</c> configurado do lado da API (confirmado ausente em todos os
/// Program.cs), enviar esses campos como string quebra a desserialização do <c>[FromBody]</c>
/// e vira "erro interno" (500) — o exemplo de NF-e da seção 4.2 do guia mostra tudo entre aspas,
/// mas o exemplo de NFC-e da seção 5.3 mostra os mesmos campos sem aspas; o segundo bate com o
/// código-fonte real (<c>TProd.cs</c>/<c>TotalModels.cs</c>/<c>ImpostoModels.cs</c> em
/// <c>PFCode\API-NF\src\Fiscal.Shared\Models</c>), o primeiro está desatualizado/incorreto.
/// </summary>
public class ProdContract
{
    [JsonPropertyName("cProd")] public string CProd { get; set; } = string.Empty;
    [JsonPropertyName("cEAN")] public string CEAN { get; set; } = "SEM GTIN";
    [JsonPropertyName("xProd")] public string XProd { get; set; } = string.Empty;
    [JsonPropertyName("NCM")] public string NCM { get; set; } = string.Empty;
    [JsonPropertyName("CEST")] public string? CEST { get; set; }
    [JsonPropertyName("CFOP")] public string CFOP { get; set; } = string.Empty;
    [JsonPropertyName("uCom")] public string UCom { get; set; } = "UN";
    [JsonPropertyName("qCom")] public decimal QCom { get; set; } = 1m;
    [JsonPropertyName("vUnCom")] public decimal VUnCom { get; set; }
    [JsonPropertyName("vProd")] public decimal VProd { get; set; }
    [JsonPropertyName("cEANTrib")] public string CEANTrib { get; set; } = "SEM GTIN";
    [JsonPropertyName("uTrib")] public string UTrib { get; set; } = "UN";
    [JsonPropertyName("qTrib")] public decimal QTrib { get; set; } = 1m;
    [JsonPropertyName("vUnTrib")] public decimal VUnTrib { get; set; }

    /// <summary>"1"=Compõe o total da nota, "0"=Não compõe.</summary>
    [JsonPropertyName("indTot")] public string IndTot { get; set; } = "1";

    // Despesas acessórias por item — sem contrapartida em ItemNotaFiscal (só existem em nível de
    // nota no domínio), usados pelo NotaFiscalPayloadBuilder para embutir frete/seguro/desconto/
    // outro da nota no primeiro item, reconciliando com o total (ver comentário em ConstruirTotal).
    [JsonPropertyName("vFrete")] public decimal? VFrete { get; set; }
    [JsonPropertyName("vSeg")] public decimal? VSeg { get; set; }
    [JsonPropertyName("vDesc")] public decimal? VDesc { get; set; }
    [JsonPropertyName("vOutro")] public decimal? VOutro { get; set; }
}

public class ImpostoContract
{
    [JsonPropertyName("ICMS")] public IcmsWrapperContract ICMS { get; set; } = new();

    /// <summary>Opcional — omitido quando o item não tem CST de IPI cadastrado (a maioria
    /// dos produtos não é sujeita a IPI). Ver <see cref="IpiWrapperContract"/>.</summary>
    [JsonPropertyName("IPI")] public IpiWrapperContract? IPI { get; set; }

    [JsonPropertyName("PIS")] public PisWrapperContract PIS { get; set; } = new();
    [JsonPropertyName("COFINS")] public CofinsWrapperContract COFINS { get; set; } = new();

    /// <summary>Obrigatório em todo item desde a Reforma Tributária (2026) — omitir
    /// derruba a emissão com cStat 1115 mesmo o XSD marcando como opcional (seção 4.10).</summary>
    [JsonPropertyName("IBSCBS")] public IbsCbsContract IBSCBS { get; set; } = new();
}

/// <summary>
/// Grupo IPI por item — espelha <c>Fiscal.Shared.Models.TImposto.IPI</c>/<c>TIpi</c> da
/// API real. O <c>JsonToXsdResolverService.DeserializeIpi</c> do lado de lá decide entre
/// "tributado" (TIpiTrib) e "não-tributado" (TIpiNT) pelo CST enviado em
/// <see cref="IpiDetailsContract"/> — mesma abordagem "um único shape flat com campos
/// nullable" já usada para ICMS/PIS/COFINS neste arquivo, deixando o CST decidir quais
/// campos vêm preenchidos.
/// </summary>
public class IpiWrapperContract
{
    /// <summary>Código de Enquadramento Legal do IPI — 3 dígitos, "999" (Outras) quando não
    /// há enquadramento específico. Obrigatório sempre que o grupo IPI é enviado.</summary>
    [JsonPropertyName("cEnq")] public string CEnq { get; set; } = "999";
    [JsonPropertyName("IPIDetails")] public IpiDetailsContract IPIDetails { get; set; } = new();
}

public class IpiDetailsContract
{
    [JsonPropertyName("CST")] public string CST { get; set; } = "53";

    /// <summary>Preenchidos só quando o CST indica tributação (ex.: 00/49/50/99) — nos CST
    /// de "não tributado" (01-05/51-55), só o CST é enviado.</summary>
    [JsonPropertyName("vBC")] public decimal? VBC { get; set; }
    [JsonPropertyName("pIPI")] public decimal? PIPI { get; set; }
    [JsonPropertyName("vIPI")] public decimal? VIPI { get; set; }
}

public class IcmsWrapperContract
{
    [JsonPropertyName("ICMSDetails")] public IcmsDetailsContract ICMSDetails { get; set; } = new();
}

/// <summary>
/// Campos de ICMS por item — cobre CST (Regime Normal: 00/10/20/40/41/50/51/60/90) e
/// CSOSN (Simples Nacional/MEI: 101/102/103/201/202/500/900), tabela da seção 4.7 do guia.
/// O <c>JsonToXsdResolverService</c> da API escolhe o tipo XML pelo CST/CSOSN preenchido
/// — envie apenas os campos exigidos pelo código usado, o resto fica null (omitido). Campos
/// numéricos são <c>decimal?</c> (número JSON), espelhando <c>TICMS00</c>/<c>TICMS10</c>/etc.
/// em <c>Fiscal.Shared.Models.ImpostoModels</c> — ver nota em <see cref="ProdContract"/>.
/// </summary>
public class IcmsDetailsContract
{
    /// <summary>Origem da mercadoria — obrigatório em todos os casos.</summary>
    [JsonPropertyName("orig")] public string Orig { get; set; } = "0";

    /// <summary>Preenchido quando o emitente é Regime Normal (CRT 2/3).</summary>
    [JsonPropertyName("CST")] public string? CST { get; set; }

    /// <summary>Preenchido quando o emitente é Simples Nacional/MEI (CRT 1/4).</summary>
    [JsonPropertyName("CSOSN")] public string? CSOSN { get; set; }

    [JsonPropertyName("modBC")] public string? ModBC { get; set; }
    [JsonPropertyName("vBC")] public decimal? VBC { get; set; }
    [JsonPropertyName("pICMS")] public decimal? PICMS { get; set; }
    [JsonPropertyName("vICMS")] public decimal? VICMS { get; set; }
    [JsonPropertyName("pRedBC")] public decimal? PRedBC { get; set; }

    // ST (CST 10 / CSOSN 201/202/500)
    [JsonPropertyName("modBCST")] public string? ModBCST { get; set; }
    [JsonPropertyName("pMVAST")] public decimal? PMVAST { get; set; }
    [JsonPropertyName("vBCST")] public decimal? VBCST { get; set; }
    [JsonPropertyName("pICMSST")] public decimal? PICMSST { get; set; }
    [JsonPropertyName("vICMSST")] public decimal? VICMSST { get; set; }
    [JsonPropertyName("vBCSTRet")] public decimal? VBCSTRet { get; set; }

    /// <summary>Alíquota do ICMS-ST retido anteriormente (NT 2016.002) — obrigatório junto de
    /// <see cref="VBCSTRet"/>/<see cref="VICMSSTRet"/> no grupo ICMS60/ICMSSN500. Confirmado
    /// numa rejeição real: "Nao informada vBCSTRet, pST e vICMSSTRet".</summary>
    [JsonPropertyName("pST")] public decimal? PST { get; set; }
    [JsonPropertyName("vICMSSTRet")] public decimal? VICMSSTRet { get; set; }

    // Diferimento (CST 51)
    [JsonPropertyName("pDif")] public decimal? PDif { get; set; }
    [JsonPropertyName("vICMSDif")] public decimal? VICMSDif { get; set; }

    // Crédito do Simples Nacional (CSOSN 101)
    [JsonPropertyName("pCredSN")] public decimal? PCredSN { get; set; }
    [JsonPropertyName("vCredICMSSN")] public decimal? VCredICMSSN { get; set; }
}

public class PisWrapperContract
{
    [JsonPropertyName("PISDetails")] public PisDetailsContract PISDetails { get; set; } = new();
}

/// <summary>CST 01/02 (alíquota), 03 (quantidade), 04-09 (não tributado), 49-99 (outros) —
/// seção 4.7 do guia.</summary>
public class PisDetailsContract
{
    [JsonPropertyName("CST")] public string CST { get; set; } = "07";
    [JsonPropertyName("vBC")] public decimal? VBC { get; set; }
    [JsonPropertyName("pPIS")] public decimal? PPIS { get; set; }
    [JsonPropertyName("vPIS")] public decimal? VPIS { get; set; }
    [JsonPropertyName("qBCProd")] public decimal? QBCProd { get; set; }
    [JsonPropertyName("vAliqProd")] public decimal? VAliqProd { get; set; }
}

public class CofinsWrapperContract
{
    [JsonPropertyName("COFINSDetails")] public CofinsDetailsContract COFINSDetails { get; set; } = new();
}

public class CofinsDetailsContract
{
    [JsonPropertyName("CST")] public string CST { get; set; } = "07";
    [JsonPropertyName("vBC")] public decimal? VBC { get; set; }
    [JsonPropertyName("pCOFINS")] public decimal? PCOFINS { get; set; }
    [JsonPropertyName("vCOFINS")] public decimal? VCOFINS { get; set; }
    [JsonPropertyName("qBCProd")] public decimal? QBCProd { get; set; }
    [JsonPropertyName("vAliqProd")] public decimal? VAliqProd { get; set; }
}

/// <summary>Grupo IBS/CBS por item — seção 4.10 do guia (Reforma Tributária, obrigatório
/// desde 2026). Caso mais comum (ad-valorem): preencher os campos direto em
/// <see cref="TribDetailsContract"/>, sem os grupos alternativos (monofásico/transferência
/// de crédito/ajuste) — não usados por este módulo.</summary>
public class IbsCbsContract
{
    /// <summary>"000" = tributação integral, sem incentivo/benefício/diferimento — caso mais comum.</summary>
    [JsonPropertyName("CST")] public string CST { get; set; } = "000";

    /// <summary>"000001" = tributação integral padrão.</summary>
    [JsonPropertyName("cClassTrib")] public string CClassTrib { get; set; } = "000001";
    [JsonPropertyName("tribDetails")] public TribDetailsContract TribDetails { get; set; } = new();
}

public class TribDetailsContract
{
    [JsonPropertyName("vBC")] public decimal VBC { get; set; }
    [JsonPropertyName("gIBSUF")] public GIbsUfItemContract GIBSUF { get; set; } = new();
    [JsonPropertyName("gIBSMun")] public GIbsMunItemContract GIBSMun { get; set; } = new();
    [JsonPropertyName("vIBS")] public decimal VIBS { get; set; }
    [JsonPropertyName("gCBS")] public GCbsItemContract GCBS { get; set; } = new();
}

public class GIbsUfItemContract
{
    [JsonPropertyName("pIBSUF")] public decimal PIBSUF { get; set; }
    [JsonPropertyName("vIBSUF")] public decimal VIBSUF { get; set; }
}

public class GIbsMunItemContract
{
    [JsonPropertyName("pIBSMun")] public decimal PIBSMun { get; set; }
    [JsonPropertyName("vIBSMun")] public decimal VIBSMun { get; set; }
}

public class GCbsItemContract
{
    [JsonPropertyName("pCBS")] public decimal PCBS { get; set; }
    [JsonPropertyName("vCBS")] public decimal VCBS { get; set; }
}
