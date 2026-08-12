namespace ImperialColors.Domain.Entities;

/// <summary>
/// Item de uma <see cref="NotaFiscal"/> — snapshot completo no momento da emissão
/// (descrição, NCM, CFOP, impostos calculados), não só uma referência a
/// <see cref="Produto"/>: o produto pode ter seu cadastro fiscal alterado depois, e a
/// nota já emitida é documento legal imutável. Espelha <c>det[].prod</c>/<c>det[].imposto</c>
/// da seção 4.6/4.7 do GUIA_INTEGRACAO.md.
/// </summary>
public class ItemNotaFiscal : BaseEntity
{
    public int NotaFiscalId { get; set; }
    public int? ProdutoId { get; set; }
    public int NItem { get; set; }

    // --- det[].prod ---
    public string CodigoProduto { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public string Cfop { get; set; } = string.Empty;
    public string Unidade { get; set; } = "UN";
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public string? UnidadeTributavel { get; set; }
    public decimal? QuantidadeTributavel { get; set; }
    public decimal? ValorUnitarioTributavel { get; set; }
    public bool CompoeTotalNota { get; set; } = true;

    // --- det[].imposto — ICMS ---
    public string? Origem { get; set; }
    public string? CstIcms { get; set; }
    public string? CsosnIcms { get; set; }
    public decimal? BaseIcms { get; set; }
    public decimal? AliquotaIcms { get; set; }
    public decimal? ValorIcms { get; set; }
    public decimal? BaseIcmsSt { get; set; }
    public decimal? AliquotaIcmsSt { get; set; }
    public decimal? ValorIcmsSt { get; set; }

    // --- PIS / COFINS ---
    public string? CstPis { get; set; }
    public decimal? BasePis { get; set; }
    public decimal? AliquotaPis { get; set; }
    public decimal? ValorPis { get; set; }
    public string? CstCofins { get; set; }
    public decimal? BaseCofins { get; set; }
    public decimal? AliquotaCofins { get; set; }
    public decimal? ValorCofins { get; set; }

    // --- IPI ---
    public string? CstIpi { get; set; }
    public decimal? BaseIpi { get; set; }
    public decimal? AliquotaIpi { get; set; }
    public decimal? ValorIpi { get; set; }

    /// <summary>Código de Enquadramento Legal do IPI (3 dígitos) — obrigatório sempre que
    /// <see cref="CstIpi"/> está preenchido, mesmo "999" (Outras) quando não há enquadramento
    /// específico. Copiado do cadastro de tributação do produto/categoria no momento da
    /// montagem do item (ver NotaFiscalService.MontarItemAPartirDeProdutoAsync).</summary>
    public string? CodigoEnquadramentoIpi { get; set; }

    // --- IBS/CBS (Reforma Tributária) ---
    public string? CstIbsCbs { get; set; }
    public string? CClassTrib { get; set; }
    public decimal? BaseIbsCbs { get; set; }
    public decimal? AliquotaIbsUf { get; set; }
    public decimal? ValorIbsUf { get; set; }
    public decimal? AliquotaIbsMunicipio { get; set; }
    public decimal? ValorIbsMunicipio { get; set; }
    public decimal? AliquotaCbs { get; set; }
    public decimal? ValorCbs { get; set; }

    public NotaFiscal NotaFiscal { get; set; } = null!;
    public Produto? Produto { get; set; }
}
