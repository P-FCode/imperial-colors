using System.Windows.Media;
using ImperialColors.Domain.Enums;

namespace ImperialColors.UI.Helpers;

/// <summary>Descrição e cor de <see cref="StatusNotaFiscal"/> para exibição — usado tanto
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

    /// <summary>
    /// Agrupa os oito status em quatro tons: verde (documento fiscal válido), vermelho
    /// (descartado — cancelado por escolha ou denegado pela SEFAZ), laranja (rejeitado —
    /// ainda dá para corrigir e reemitir) e cinza/amarelo (situação transitória, sem ação
    /// definida ainda). As mesmas cores pastel já usadas em <c>VendasView</c> para o status
    /// da venda, para o app não ganhar uma paleta de badge nova a cada tela.
    /// </summary>
    public static Brush CorFundo(StatusNotaFiscal status) => status switch
    {
        StatusNotaFiscal.Autorizada => Brush("#D4EDDA"),
        StatusNotaFiscal.Cancelada or StatusNotaFiscal.Denegada => Brush("#F8D7DA"),
        StatusNotaFiscal.Rejeitada => Brush("#FDEBD3"),
        StatusNotaFiscal.Rascunho or StatusNotaFiscal.Inutilizada => Brush("#E9ECEF"),
        _ => Brush("#FFF3CD") // Emitindo, Indeterminada: em andamento/aguardando confirmação.
    };

    /// <summary>Texto escuro correspondente a cada <see cref="CorFundo"/>, com contraste
    /// suficiente sobre o pastel.</summary>
    public static Brush CorTexto(StatusNotaFiscal status) => status switch
    {
        StatusNotaFiscal.Autorizada => Brush("#155724"),
        StatusNotaFiscal.Cancelada or StatusNotaFiscal.Denegada => Brush("#721C24"),
        StatusNotaFiscal.Rejeitada => Brush("#8A4B08"),
        StatusNotaFiscal.Rascunho or StatusNotaFiscal.Inutilizada => Brush("#495057"),
        _ => Brush("#856404")
    };

    private static Brush Brush(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }
}
