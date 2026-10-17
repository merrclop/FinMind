using System.IO;

namespace FinMind.Services;

/// <summary>Шляхи до локальних даних програми (%LocalAppData%\FinMind).</summary>
public static class AppPaths
{
    public static string Folder
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FinMind");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
