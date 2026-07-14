using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ImperialColors.UI.Helpers;

public static class BarcodeScanHelper
{
    private static readonly SolidColorBrush BordaErro = new(Color.FromRgb(0xDC, 0x35, 0x45));
    private static readonly SolidColorBrush BordaNormal = new(Color.FromRgb(0xDE, 0xDE, 0xDE));

    public static void ProdutoNaoEncontrado(TextBox campo, TextBlock? aviso = null)
    {
        SystemSounds.Hand.Play();
        aviso?.SetCurrentValue(TextBlock.TextProperty, "Produto não encontrado");
        aviso?.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Visible);

        campo.Clear();
        campo.Focus();

        PiscarBordaErro(campo);

        if (aviso is null) return;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            aviso.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed);
        };
        timer.Start();
    }

    private static void PiscarBordaErro(TextBox campo)
    {
        if (campo.Template?.FindName("Borda", campo) is Border borda)
        {
            borda.BorderBrush = BordaErro;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                borda.BorderBrush = BordaNormal;
            };
            timer.Start();
            return;
        }

        campo.BorderBrush = BordaErro;
        var fallback = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
        fallback.Tick += (_, _) =>
        {
            fallback.Stop();
            campo.ClearValue(Control.BorderBrushProperty);
        };
        fallback.Start();
    }
}
