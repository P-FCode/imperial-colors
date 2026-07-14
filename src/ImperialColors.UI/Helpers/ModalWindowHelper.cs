using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ImperialColors.UI.Helpers;

public static class ModalWindowHelper
{
    public static void AplicarEstiloModerno(Window janela)
    {
        janela.WindowStyle = WindowStyle.None;
        janela.AllowsTransparency = true;
        janela.Background = Brushes.Transparent;
        janela.ShowInTaskbar = false;
        janela.ResizeMode = ResizeMode.NoResize;

        janela.Loaded += (_, _) => ConfigurarArrastar(janela);
    }

    /// <summary>
    /// Define Owner e posicionamento ANTES de ShowDialog().
    /// </summary>
    public static void PrepararDialogo(Window janela, Window? owner = null)
    {
        janela.Owner = owner ?? ObterJanelaProprietaria(janela);
        janela.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    public static bool? ExibirDialogo(Window janela, Window? owner = null)
    {
        PrepararDialogo(janela, owner);
        return janela.ShowDialog();
    }

    public static void Fechar(Window janela, bool? resultado = false)
    {
        if (!janela.IsLoaded) return;
        janela.DialogResult = resultado;
        janela.Close();
    }

    private static Window? ObterJanelaProprietaria(Window janelaExcluir)
    {
        var app = System.Windows.Application.Current;
        if (app is null) return null;

        var ativa = app.Windows.OfType<Window>()
            .FirstOrDefault(w => w.IsActive && w.IsLoaded && !ReferenceEquals(w, janelaExcluir));

        if (ativa is not null)
            return ativa;

        var principal = app.MainWindow;
        return principal is { IsLoaded: true } && !ReferenceEquals(principal, janelaExcluir)
            ? principal
            : null;
    }

    private static void ConfigurarArrastar(Window janela)
    {
        var cabecalho = janela.FindName("BorderCabecalho") as UIElement
                        ?? janela.FindName("TxtTitulo") as UIElement;

        if (cabecalho is null) return;

        cabecalho.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                janela.DragMove();
        };
    }
}
