using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.DTOs;

/// <summary>
/// Regra fiscal reutilizável por tipo de operação (venda dentro do estado, venda para
/// outro estado, devolução...). Equivalente a "Natureza de Operação" no Olist.
/// </summary>
public class NaturezaOperacaoDto
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public TipoOperacaoFiscal TipoOperacao { get; set; } = TipoOperacaoFiscal.Saida;
    public FinalidadeNfe Finalidade { get; set; } = FinalidadeNfe.Normal;
    public bool ConsumidorFinal { get; set; } = true;
    public string? Serie { get; set; }

    public string? CsosnPadrao { get; set; }
    public string? CstIcmsPadrao { get; set; }

    public string? CfopDentroEstado { get; set; }
    public string? CfopForaEstado { get; set; }

    public bool? DifalNaoContribuinte { get; set; }
    public string? ObservacoesPadrao { get; set; }
}
