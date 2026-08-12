using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

/// <summary>
/// Histórico de eventos de uma <see cref="NotaFiscal"/> — emissão, cancelamento, carta de
/// correção, inutilização e consultas de status. Alimenta a janela "Ações da Nota" e
/// cobre o <c>eventosRegistrados</c> descrito na seção 7.1 do GUIA_INTEGRACAO.md.
/// </summary>
public class NotaFiscalEvento : BaseEntity
{
    public int NotaFiscalId { get; set; }
    public TipoEventoNotaFiscal Tipo { get; set; }
    public DateTime DataHora { get; set; } = DateTime.Now;
    public bool Sucesso { get; set; }

    /// <summary>Protocolo do evento (distinto do protocolo de autorização da nota) —
    /// não se aplica a Inutilização, que não gera chave/protocolo vinculável.</summary>
    public string? NProtEvento { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }

    /// <summary>Texto livre do evento — justificativa (cancelamento/inutilização) ou
    /// texto de correção (CC-e).</summary>
    public string? Texto { get; set; }
    public int? Sequencial { get; set; }
    public string? TraceId { get; set; }
    public string? Usuario { get; set; }

    public NotaFiscal NotaFiscal { get; set; } = null!;
}
