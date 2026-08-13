namespace ImperialColors.Application.DTOs;

/// <summary>
/// Totais fiscais de uma venda, equivalentes a <c>total.ICMSTot</c> e
/// <c>total.IBSCBSTot</c> do XML da NF-e/NFC-e — calculados a partir dos itens da venda
/// e da tributação cadastrada de cada produto. Nomes de campo seguem a tag oficial do
/// XML (vBC, vICMS, vIBSUF...) de propósito, para a futura montagem do payload ser um
/// mapeamento direto, não uma tradução.
///
/// Isto NÃO substitui o `MathematicalValidator` da API de emissão — é uma conferência
/// prévia, pra pegar erro de cadastro (produto sem tributação, CST sem alíquota) antes
/// de chegar na hora de emitir.
/// </summary>
public class TotaisFiscaisVendaDto
{
    public int VendaId { get; set; }
    public string NumeroVenda { get; set; } = string.Empty;

    // --- Equivalente a total.ICMSTot ---
    public decimal VProd { get; set; }
    public decimal VDesc { get; set; }
    public decimal VBcIcms { get; set; }
    public decimal VIcms { get; set; }

    /// <summary>ICMS-ST "para frente" (CST 10 / CSOSN 201/202/203).</summary>
    public decimal VBcIcmsSt { get; set; }
    public decimal VIcmsSt { get; set; }

    /// <summary>ICMS-ST retido anteriormente (CST 60 / CSOSN 500) — informativo, não entra
    /// no vNF nem em nenhum total oficial do XML (o leiaute não tem um totalizador próprio
    /// para vICMSSTRet).</summary>
    public decimal VBcIcmsStRetido { get; set; }
    public decimal VIcmsStRetido { get; set; }

    public decimal VBcPis { get; set; }
    public decimal VPis { get; set; }
    public decimal VBcCofins { get; set; }
    public decimal VCofins { get; set; }

    /// <summary>vNF = vProd - vDesc (sem frete/seguro/outras despesas/IPI — não modelados na venda hoje).</summary>
    public decimal VNf { get; set; }

    // --- Equivalente a total.IBSCBSTot ---
    public decimal VBcIbsCbs { get; set; }
    public decimal VIbsUf { get; set; }
    public decimal VIbsMunicipio { get; set; }

    /// <summary>vIBS = vIBSUF + vIBSMun.</summary>
    public decimal VIbs { get; set; }
    public decimal VCbs { get; set; }

    /// <summary>
    /// Itens/situações que o cálculo automático não cobre (Substituição Tributária,
    /// CST sem regra mapeada, produto sem tributação cadastrada) — revisar manualmente
    /// antes de confiar no total para emissão real.
    /// </summary>
    public List<string> Avisos { get; set; } = new();

    public List<ItemCalculoFiscalDto> Itens { get; set; } = new();
}

/// <summary>Detalhamento do cálculo fiscal de um item da venda — útil para conferência.</summary>
public class ItemCalculoFiscalDto
{
    public int ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public decimal ValorItem { get; set; }

    /// <summary>Base de cálculo do ICMS já com a redução de base aplicada (pRedBC do
    /// cadastro do produto) — só preenchida quando o CST destaca ICMS. Sem isso, quem
    /// consome o cálculo teria que reaplicar a redução por conta própria para chegar ao
    /// vBC que vai no XML.</summary>
    public decimal? VBcIcms { get; set; }
    public decimal VIcms { get; set; }

    /// <summary>Bases de PIS/COFINS — preenchidas só quando o CST tributa. Os grupos
    /// PISAliq/COFINSAliq do XML exigem <c>vBC</c> junto de <c>pPIS</c>/<c>vPIS</c>;
    /// enviar alíquota e valor sem base derruba a nota por XML incompleto.</summary>
    public decimal? VBcPis { get; set; }
    public decimal VPis { get; set; }
    public decimal? VBcCofins { get; set; }
    public decimal VCofins { get; set; }
    public decimal? VIpi { get; set; }

    /// <summary>ICMS-ST "para frente" (CST 10 / CSOSN 201/202/203) — calculado a partir do
    /// MVA cadastrado. Preenchido só quando o CST/CSOSN exige o grupo.</summary>
    public decimal? VBcIcmsSt { get; set; }
    public decimal VIcmsSt { get; set; }

    /// <summary>ICMS-ST retido anteriormente (CST 60 / CSOSN 500) — declarado a partir do
    /// percentual pST cadastrado, não calculado por fórmula de margem.</summary>
    public decimal? VBcIcmsStRetido { get; set; }
    public decimal VIcmsStRetido { get; set; }

    public decimal VIbsUf { get; set; }
    public decimal VIbsMunicipio { get; set; }
    public decimal VCbs { get; set; }
    public List<string> Avisos { get; set; } = new();
}
