using System.Windows;
using Microsoft.Win32;

namespace FinMind.Services;

/// <summary>Діалоги підтвердження та вибору файлів.</summary>
public static class DialogService
{
    public static bool Confirm(string message, string title = "Підтвердження") =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public static string? PickImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Оберіть фото чека",
            Filter = "Зображення|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.gif|Усі файли|*.*"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public static string? PickCsvSavePath(string defaultName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Експорт у CSV",
            FileName = defaultName,
            Filter = "CSV (для Excel)|*.csv",
            DefaultExt = ".csv"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
