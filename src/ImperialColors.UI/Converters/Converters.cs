using ImperialColors.UI.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ImperialColors.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

public class InvertBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

public class MoedaConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d) return FormattingHelper.FormatarMoeda(d);
        if (value is double dbl) return FormattingHelper.FormatarMoeda((decimal)dbl);
        if (value is int i) return FormattingHelper.FormatarMoeda(i);
        return FormattingHelper.FormatarMoeda(0m);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && FormattingHelper.TryParseMoeda(s, out decimal result))
            return result;
        return 0m;
    }
}

public class DataConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime data)
            return FormattingHelper.FormatarData(data);
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class DataHoraConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime data)
            return FormattingHelper.FormatarDataHora(data);
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class QuantidadeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal quantidade)
            return FormattingHelper.FormatarQuantidade(quantidade);

        if (value is double quantidadeDouble)
            return FormattingHelper.FormatarQuantidade((decimal)quantidadeDouble);

        if (decimal.TryParse(value?.ToString(), NumberStyles.Number, FormattingHelper.CulturaPtBr, out var qtd))
            return FormattingHelper.FormatarQuantidade(qtd);

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class QuantidadeUnidadeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return string.Empty;

        if (values[0] is decimal quantidade)
            return FormattingHelper.FormatarQuantidadeUnidade(quantidade, values[1]?.ToString());

        if (values[0] is double quantidadeDouble)
            return FormattingHelper.FormatarQuantidadeUnidade((decimal)quantidadeDouble, values[1]?.ToString());

        if (decimal.TryParse(values[0]?.ToString(), NumberStyles.Number, FormattingHelper.CulturaPtBr, out var qtd))
            return FormattingHelper.FormatarQuantidadeUnidade(qtd, values[1]?.ToString());

        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class EstoqueStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? new SolidColorBrush(Color.FromRgb(220, 53, 69)) : new SolidColorBrush(Color.FromRgb(40, 167, 69));
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class DecimalToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d.ToString("G", FormattingHelper.CulturaPtBr);
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && FormattingHelper.TryParseQuantidade(s, out decimal result))
            return result;
        return 0m;
    }
}
