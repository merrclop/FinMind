using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using FinMind.Data;
using FinMind.Services;

namespace FinMind;

/// <summary>Точка входу: культура, налаштування, тема, БД, головне вікно.</summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Глобальний захист від необроблених винятків UI — програма не падає, а показує повідомлення.
        DispatcherUnhandledException += (_, args) =>
        {
            NotificationService.Error($"Непередбачена помилка: {args.Exception.Message}");
            args.Handled = true;
        };

        // Українська культура: формати чисел/дат, календар у DatePicker.
        var ua = new CultureInfo("uk-UA");
        CultureInfo.DefaultThreadCurrentCulture = ua;
        CultureInfo.DefaultThreadCurrentUICulture = ua;
        Thread.CurrentThread.CurrentCulture = ua;
        Thread.CurrentThread.CurrentUICulture = ua;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("uk-UA")));

        try
        {
            await SettingsService.Instance.LoadAsync();
            ThemeManager.Apply(SettingsService.Instance.Current.Theme);
            await DatabaseInitializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося запустити FinMind:\n{ex.Message}", "FinMind",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }
}
