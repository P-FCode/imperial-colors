using ImperialColors.Domain.Enums;

namespace ImperialColors.UI.Helpers;

/// <summary>Descrição em PT-BR de <see cref="StatusNotaFiscal"/> para exibição — usado tanto
/// pela lista de notas (<c>NotaFiscalListaView</c>) quanto pelo painel-resumo do hub
/// (<c>NotaFiscalHubView</c>), evitando duas cópias do mesmo switch.</summary>
public static class NotaFiscalStatusHelper
{
    public static string Descricao(StatusNotaFiscal status) => status switch
    {
        StatusNotaFiscal.Rascunho => "Rascunho",
        StatusNotaFiscal.Emitindo => "Emitindo",
        StatusNotaFiscal.Autorizada => "Autorizada",
        StatusNotaFiscal.Rejeitada => "Rejeitada",
        StatusNotaFiscal.Cancelada => "Cancelada",
        StatusNotaFiscal.Denegada => "Denegada",
        StatusNotaFiscal.Inutilizada => "Inutilizada",
        StatusNotaFiscal.Indeterminada => "Indeterminada (consulte o status)",
        _ => status.ToString()
    };
}
