using System.Globalization;
using System.IO;
using System.Text;
using FinMind.Models;

namespace FinMind.Services;

/// <summary>Експорт транзакцій у CSV (роздільник «;», UTF-8 з BOM — коректно відкривається в українському Excel).</summary>
public class ExportService
{
    private static readonly CultureInfo Ua = new("uk-UA");

    public async Task ExportCsvAsync(IEnumerable<Transaction> items, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Дата;Тип;Категорія;Опис;Сума");
        foreach (var t in items)
        {
            sb.Append(t.Date.ToString("dd.MM.yyyy", Ua)).Append(';')
              .Append(t.Type == TransactionType.Income ? "Дохід" : "Витрата").Append(';')
              .Append(Escape(t.Category?.Name)).Append(';')
              .Append(Escape(t.Description)).Append(';')
              .AppendLine(t.Amount.ToString("0.00", Ua));
        }
        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(true));
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.IndexOfAny([';', '"', '\n', '\r']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
