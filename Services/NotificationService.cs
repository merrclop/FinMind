namespace FinMind.Services;

/// <summary>Прості «тости»: ViewModel-і шлють повідомлення, MainWindow їх показує.</summary>
public static class NotificationService
{
    public static event Action<string, bool>? Raised;

    public static void Info(string message) => Raised?.Invoke(message, false);
    public static void Error(string message) => Raised?.Invoke(message, true);
}
