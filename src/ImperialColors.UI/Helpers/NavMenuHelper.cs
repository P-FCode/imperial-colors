using System.Windows;

namespace ImperialColors.UI.Helpers;

public static class NavMenuHelper
{
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.RegisterAttached(
            "IsActive",
            typeof(bool),
            typeof(NavMenuHelper),
            new PropertyMetadata(false));

    public static void SetIsActive(DependencyObject element, bool value)
        => element.SetValue(IsActiveProperty, value);

    public static bool GetIsActive(DependencyObject element)
        => (bool)element.GetValue(IsActiveProperty);
}
