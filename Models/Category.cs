namespace FinMind.Models;

/// <summary>Категорія транзакцій (Їжа, Транспорт, ...).</summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Колір у форматі #RRGGBB (для діаграми та міток).</summary>
    public string ColorHex { get; set; } = "#808080";
    /// <summary>Емодзі-іконка.</summary>
    public string Icon { get; set; } = "📦";

    public override string ToString() => Name;
}
