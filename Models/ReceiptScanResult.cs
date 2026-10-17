namespace FinMind.Models;

/// <summary>Результат розпізнавання фото чека (Gemini Vision).</summary>
public class ReceiptScanResult
{
    public decimal TotalAmount { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
}

/// <summary>Результат розбору текстового опису («Кава 120 грн») через Gemini.</summary>
public class ParsedTransaction
{
    public decimal Amount { get; set; }
    public bool IsIncome { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
}
