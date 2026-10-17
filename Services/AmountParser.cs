using System.Globalization;

namespace FinMind.Services;

/// <summary>Розбір суми з рядка: приймає і «1 250,50», і «1250.5».</summary>
public static class AmountParser
{
    public static bool TryParse(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Replace(" ", "").Replace("\u00A0", "").Replace(',', '.');
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
