using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ImperialColors.UI.Helpers;

public static class LogoHelper
{
    private static readonly ConcurrentDictionary<string, BitmapImage> CacheImagens = new(StringComparer.OrdinalIgnoreCase);

    public static void AplicarLogo(Image image, string caminho, double? largura = null, double? altura = null)
    {
        if (!File.Exists(caminho))
        {
            image.Visibility = Visibility.Collapsed;
            return;
        }

        image.Source = ObterOuCarregarImagem(caminho);
        image.Visibility = Visibility.Visible;

        if (largura.HasValue)
            image.Width = largura.Value;
        if (altura.HasValue)
            image.Height = altura.Value;
    }

    public static void AplicarIconeJanela(Window window, string caminho)
    {
        if (!File.Exists(caminho))
            return;

        window.Icon = BitmapFrame.Create(new Uri(caminho, UriKind.Absolute));
    }

    private static BitmapImage ObterOuCarregarImagem(string caminho)
        => CacheImagens.GetOrAdd(caminho, static path =>
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        });
}
