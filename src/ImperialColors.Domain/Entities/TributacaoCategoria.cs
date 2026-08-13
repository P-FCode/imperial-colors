using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

/// <summary>
/// Perfil fiscal padrão de uma categoria (NCM, CST/CSOSN, PIS/COFINS, IPI) — usado para
/// pré-preencher a tributação de produtos novos cadastrados nessa categoria, evitando
/// redigitar os mesmos códigos fiscais para cada tinta/verniz/etc. do mesmo grupo. O
/// produto sempre pode sobrescrever o valor herdado antes de salvar. Relação 1:1 opcional
/// com <see cref="Categoria"/> (chave primária compartilhada: <see cref="CategoriaId"/>).
///
/// Não inclui GTIN/unidade tributável/fator de conversão — são específicos de cada
/// produto (código de barras único, embalagem) e não fazem sentido como padrão de grupo.
/// </summary>
public class TributacaoCategoria
{
    public int CategoriaId { get; set; }

    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public OrigemMercadoria? Origem { get; set; }

    public string? CstIcms { get; set; }
    public string? CsosnIcms { get; set; }
    public decimal? AliquotaIcms { get; set; }
    public decimal? AliquotaIcmsSt { get; set; }
    public decimal? Mva { get; set; }
    public decimal? ReducaoBaseCalculo { get; set; }

    /// <summary>Percentual do ICMS-ST retido (<c>pST</c>) — ver observações em
    /// <see cref="TributacaoProduto.AliquotaIcmsStRetido"/>.</summary>
    public decimal? AliquotaIcmsStRetido { get; set; }

    public string? CstPis { get; set; }
    public decimal? AliquotaPis { get; set; }

    public string? CstCofins { get; set; }
    public decimal? AliquotaCofins { get; set; }

    public string? CstIpi { get; set; }
    public string? CodigoEnquadramentoIpi { get; set; }
    public decimal? AliquotaIpi { get; set; }

    /// <summary>CFOP para venda dentro do estado do emitente — normalmente começa com 5.</summary>
    public string? CfopDentroEstado { get; set; }

    /// <summary>CFOP para venda para outro estado — normalmente começa com 6.</summary>
    public string? CfopForaEstado { get; set; }

    // --- Reforma Tributária (IBS/CBS) — ver observações em TributacaoProduto ---
    public string? CstIbsCbs { get; set; }
    public string? CClassTrib { get; set; }
    public string? CstIS { get; set; }
    public string? CClassTribIS { get; set; }
    public decimal? AliquotaIS { get; set; }
    public decimal? AliquotaIbsMunicipioDiferimento { get; set; }
    public decimal? AliquotaIbsMunicipioReducao { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public Categoria Categoria { get; set; } = null!;
}
