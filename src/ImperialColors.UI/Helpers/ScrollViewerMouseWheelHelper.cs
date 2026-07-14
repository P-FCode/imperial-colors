using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Helpers;

public static class ScrollViewerMouseWheelHelper
{
    public static readonly DependencyProperty HabilitarProperty =
        DependencyProperty.RegisterAttached(
            "Habilitar",
            typeof(bool),
            typeof(ScrollViewerMouseWheelHelper),
            new PropertyMetadata(false, OnHabilitarChanged));

    public static void SetHabilitar(DependencyObject element, bool value) => element.SetValue(HabilitarProperty, value);
    public static bool GetHabilitar(DependencyObject element) => (bool)element.GetValue(HabilitarProperty);

    public static void Anexar(ScrollViewer scrollViewer)
    {
        scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
        scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
    }

    private static void OnHabilitarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
            return;

        scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;

        if (e.NewValue is true)
            scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
    }

    private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
            return;

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}
