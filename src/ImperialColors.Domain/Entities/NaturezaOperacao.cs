using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

/// <summary>
/// Regra fiscal reutilizável por tipo de operação (venda dentro do estado, venda para
/// outro estado, devolução...) — evita redigitar CSOSN/CST/CFOP/finalidade toda vez que
/// uma nota for montada. Equivalente a "Natureza de Operação" no Olist. Preparação de
/// dado: hoje só é cadastro, nada aqui monta ou emite uma nota sozinho.
/// </summary>
public class NaturezaOperacao : BaseEntity
{
    public string Descricao { get; set; } = string.Empty;
    public TipoOperacaoFiscal TipoOperacao { get; set; } = TipoOperacaoFiscal.Saida;
    public FinalidadeNfe Finalidade { get; set; } = FinalidadeNfe.Normal;
    public bool ConsumidorFinal { get; set; } = true;

    /// <summary>Série a usar quando esta natureza for aplicada — em branco usa a Série
    /// padrão da empresa (Configurações → Fiscal).</summary>
    public string? Serie { get; set; }

    // CST × CSOSN seguem a mesma regra mutuamente exclusiva do cadastro de produto —
    // conforme o regime tributário da empresa, só um dos dois deve ser preenchido.
    public string? CsosnPadrao { get; set; }
    public string? CstIcmsPadrao { get; set; }

    public string? CfopDentroEstado { get; set; }
    public string? CfopForaEstado { get; set; }

    /// <summary>Confirma/sobrescreve o toggle geral de DIFAL (Configurações → Fiscal)
    /// para esta natureza específica.</summary>
    public bool? DifalNaoContribuinte { get; set; }

    /// <summary>Texto que entra automaticamente nas observações de notas desta operação.</summary>
    public string? ObservacoesPadrao { get; set; }
}
