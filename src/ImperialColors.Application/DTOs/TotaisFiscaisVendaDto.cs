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
    public decimal VIcms { get; set; }
    public decimal VPis { get; set; }
    public decimal VCofins { get; set; }
    public decimal VIbsUf { get; set; }
    public decimal VIbsMunicipio { get; set; }
    public decimal VCbs { get; set; }
    public List<string> Avisos { get; set; } = new();
}
