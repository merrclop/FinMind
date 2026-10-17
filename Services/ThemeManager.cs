using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FinMind.Services;

/// <summary>Перемикання темної/світлої теми (заміна ResourceDictionary) + темний заголовок вікна Windows.</summary>
public static class ThemeManager
{
    public static string Current { get; private set; } = "Dark";

    public static void Apply(string theme)
    {
        Current = theme == "Light" ? "Light" : "Dark";
        var dict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{Current}Theme.xaml", UriKind.Absolute)
        };

        var merged = Application.Current.Resources.MergedDictionaries;
        if (merged.Count > 0 && merged[0].Source?.OriginalString.Contains("Theme.xaml") == true)
            merged[0] = dict;
        else
            merged.Insert(0, dict);

        foreach (Window w in Application.Current.Windows)
            ApplyTitleBar(w);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    /// <summary>Темна/світла смуга заголовка (Windows 10 20H1+/11).</summary>
    public static void ApplyTitleBar(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;
            int dark = Current == "Dark" ? 1 : 0;
            DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        }
        catch { /* не критично */ }
    }
}
