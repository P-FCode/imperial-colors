using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ImperialColors.UI.Helpers;

/// <summary>
/// Garante que o texto do DatePicker seja atualizado ao selecionar uma data no calendário popup.
/// Templates customizados podem quebrar a sincronização interna do DatePickerTextBox.
/// </summary>
public static class DatePickerSyncHelper
{
    public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
        "Enable",
        typeof(bool),
        typeof(DatePickerSyncHelper),
        new PropertyMetadata(false, OnEnableChanged));

    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);
    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DatePicker picker)
            return;

        if ((bool)e.NewValue)
        {
            picker.SelectedDateChanged += OnSelectedDateChanged;
            picker.Loaded += OnPickerLoaded;
        }
        else
        {
            picker.SelectedDateChanged -= OnSelectedDateChanged;
            picker.Loaded -= OnPickerLoaded;
        }
    }

    private static void OnPickerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker picker)
            SincronizarTexto(picker);
    }

    private static void OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DatePicker picker)
            picker.Dispatcher.BeginInvoke(() => SincronizarTexto(picker));
    }

    internal static void SincronizarTexto(DatePicker picker)
    {
        if (!picker.IsLoaded)
            return;

        picker.ApplyTemplate();

        if (picker.Template.FindName("PART_TextBox", picker) is not DatePickerTextBox textBoxHost)
            return;

        textBoxHost.ApplyTemplate();

        if (textBoxHost.Template.FindName("PART_TextBox", textBoxHost) is not TextBox innerTextBox)
            return;

        var texto = picker.SelectedDate.HasValue
            ? picker.SelectedDate.Value.ToString("d", CultureInfo.CurrentCulture)
            : string.Empty;

        if (!string.Equals(innerTextBox.Text, texto, StringComparison.Ordinal))
            innerTextBox.Text = texto;
    }
}
