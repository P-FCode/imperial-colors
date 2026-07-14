using System.Printing;
using System.Windows;
using System.Windows.Controls;

namespace ImperialColors.UI.Helpers;

public static class CupomPrintHelper
{
    public static void PrepararVisualParaImpressao(FrameworkElement visual)
    {
        visual.UpdateLayout();

        var largura = visual.ActualWidth > 0 ? visual.ActualWidth : 392;
        visual.Measure(new Size(largura, double.PositiveInfinity));
        visual.Arrange(new Rect(0, 0, largura, visual.DesiredSize.Height));
        visual.UpdateLayout();
    }

    public static bool ImprimirNaImpressoraConfigurada(
        FrameworkElement visual,
        string nomeDocumento,
        string? nomeImpressora,
        out string? mensagemErro)
    {
        mensagemErro = null;

        try
        {
            PrepararVisualParaImpressao(visual);

            var dialog = new PrintDialog();

            if (!string.IsNullOrWhiteSpace(nomeImpressora))
            {
                var server = new LocalPrintServer();
                dialog.PrintQueue = server.GetPrintQueue(nomeImpressora);
            }
            else
            {
                mensagemErro = "Nenhuma impressora configurada. Acesse Configurações → Periféricos.";
                return false;
            }

            dialog.PrintVisual(visual, nomeDocumento);
            return true;
        }
        catch (PrintQueueException ex)
        {
            mensagemErro = $"Impressora '{nomeImpressora}' indisponível: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            mensagemErro = $"Erro ao imprimir: {ex.Message}";
            return false;
        }
    }
}
