using System.Windows;

namespace ImperialColors.UI.Helpers;

/// <summary>
/// Propriedade attached que define o texto de espaço reservado (placeholder) exibido
/// dentro de um TextBox quando ele está vazio. Usada pelo template do estilo
/// "TextBoxFiltro" (AppTheme.xaml) para alinhar o placeholder exatamente com o
/// Padding do campo, evitando que o texto digitado fique desalinhado em relação
/// ao placeholder exibido antes da digitação.
/// </summary>
public static class PlaceholderHelper
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(PlaceholderHelper),
        new PropertyMetadata(string.Empty));

    public static string GetText(DependencyObject element) => (string)element.GetValue(TextProperty);
    public static void SetText(DependencyObject element, string value) => element.SetValue(TextProperty, value);
}
