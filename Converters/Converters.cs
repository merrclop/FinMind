using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FinMind.Models;
using FinMind.Services;

namespace FinMind.Converters;

/// <summary>bool → Visibility. ConverterParameter="Invert" інвертує.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var b = value is bool v && v;
        if (parameter as string == "Invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>object → Visibility (видимий, якщо не null). "Invert" — навпаки.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var b = value != null;
        if (parameter as string == "Invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !(value is bool b && b);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => !(value is bool b && b);
}

/// <summary>Порівняння value.ToString() з ConverterParameter (для підсвітки активного пункту меню).</summary>
public class EqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>"#RRGGBB" → SolidColorBrush.</summary>
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is string hex && ColorConverter.ConvertFromString(hex) is Color c)
                return new SolidColorBrush(c);
        }
        catch { /* fallback нижче */ }
        return Brushes.Gray;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>decimal → «1 234,56 ₴» (символ береться з налаштувань валюти).</summary>
public class CurrencyConverter : IValueConverter
{
    private static readonly CultureInfo Ua = new("uk-UA");

    public static string Format(decimal amount) =>
        $"{amount.ToString("N2", Ua)} {SettingsService.Instance.CurrencySymbol}";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        decimal d => Format(d),
        double dbl => Format((decimal)dbl),
        _ => string.Empty
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>DateTime → «13.10.2026» (параметр "long" → «13 жовтня 2026»).</summary>
public class DateConverter : IValueConverter
{
    private static readonly CultureInfo Ua = new("uk-UA");
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime d) return string.Empty;
        return d.ToString(parameter as string == "long" ? "d MMMM yyyy" : "dd.MM.yyyy", Ua);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Transaction → «+25 000,00 ₴» / «−120,00 ₴».</summary>
public class TransactionAmountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Transaction t) return string.Empty;
        return (t.Type == TransactionType.Income ? "+" : "−") + CurrencyConverter.Format(t.Amount);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
