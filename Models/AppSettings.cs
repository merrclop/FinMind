namespace FinMind.Models;

/// <summary>Налаштування, що зберігаються у settings.json (ключ API не захардкоджений).</summary>
public class AppSettings
{
    public string GeminiApiKey { get; set; } = string.Empty;
    /// <summary>Назва моделі Gemini. Змінюється в налаштуваннях, якщо Google оновить моделі.</summary>
    public string GeminiModel { get; set; } = "gemini-2.5-flash";
    /// <summary>UAH / USD / EUR.</summary>
    public string Currency { get; set; } = "UAH";
    /// <summary>Dark / Light.</summary>
    public string Theme { get; set; } = "Dark";
    /// <summary>Масштаб інтерфейсу (1.0 = 100%).</summary>
    public double UiScale { get; set; } = 1.0;
}
