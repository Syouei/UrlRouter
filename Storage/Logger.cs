using System.Text;

namespace UrlRouter.Storage;

internal static class Logger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UrlRouter", "log.txt");

    public static void Log(string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{line}";
            File.AppendAllText(LogPath, entry + Environment.NewLine, Encoding.UTF8);
            TrimIfNeeded();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logger failed: {ex.Message}");
        }
    }

    public static void Open(string browserExe, string url) => Log($"OPEN\t{browserExe}\t{url}");
    public static void Error(string source, Exception ex, string? url = null) =>
        Log($"ERROR\t{source}\t{ex.GetType().Name}\t{ex.Message}\t{url}");
    public static void Warn(string source, string message) => Log($"WARN\t{source}\t{message}");

    private static void TrimIfNeeded()
    {
        try
        {
            var fi = new FileInfo(LogPath);
            if (fi.Length <= 1024 * 1024) return;

            var lines = File.ReadAllLines(LogPath, Encoding.UTF8);
            var half = lines.Length / 2;
            var trimmed = lines.Skip(half).ToArray();
            File.WriteAllLines(LogPath, trimmed, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Log trim failed: {ex.Message}");
        }
    }
}
