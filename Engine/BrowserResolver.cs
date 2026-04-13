using System.Diagnostics;
using Microsoft.Win32;
using UrlRouter.Models;
using UrlRouter.Storage;

namespace UrlRouter.Engine;

internal static class BrowserResolver
{
    public static string? Resolve(BrowserTarget target)
    {
        if (target.Kind == BrowserKind.Custom && !string.IsNullOrWhiteSpace(target.CustomExePath))
        {
            if (File.Exists(target.CustomExePath))
                return target.CustomExePath;
            Logger.Warn("BrowserResolver", $"Custom exe not found: {target.CustomExePath}");
        }

        return FindExe(target.Kind) ?? FindExe(BrowserKind.Edge) ?? FindExe(BrowserKind.Chrome) ?? FindExe(BrowserKind.Firefox);
    }

    private static string? FindExe(BrowserKind kind)
    {
        var exeName = kind switch
        {
            BrowserKind.Edge => "msedge.exe",
            BrowserKind.Chrome => "chrome.exe",
            BrowserKind.Firefox => "firefox.exe",
            _ => null
        };

        if (exeName == null) return null;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}");
            var path = key?.GetValue("") as string;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) return path;
        }
        catch (Exception ex)
        {
            Logger.Warn("FindExe-regLM", ex.Message);
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}");
            var path = key?.GetValue("") as string;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) return path;
        }
        catch (Exception ex)
        {
            Logger.Warn("FindExe-regCU", ex.Message);
        }

        var candidates = GetCandidatePaths(kind);
        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in envPath.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var p = Path.Combine(dir.Trim(), exeName);
                if (File.Exists(p)) return p;
            }
            catch (Exception ex)
            {
                Logger.Warn("FindExe-path", ex.Message);
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidatePaths(BrowserKind kind)
    {
        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return kind switch
        {
            BrowserKind.Edge => new[]
            {
                Path.Combine(pf86, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(pf, "Microsoft", "Edge", "Application", "msedge.exe")
            },
            BrowserKind.Chrome => new[]
            {
                Path.Combine(pf86, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(pf, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(local, "Google", "Chrome", "Application", "chrome.exe")
            },
            BrowserKind.Firefox => new[]
            {
                Path.Combine(pf86, "Mozilla Firefox", "firefox.exe"),
                Path.Combine(pf, "Mozilla Firefox", "firefox.exe")
            },
            _ => Array.Empty<string>()
        };
    }

    public static void LaunchBrowser(string browserExe, string url)
    {
        var psi = new ProcessStartInfo
        {
            FileName = browserExe,
            Arguments = url,
            UseShellExecute = true
        };
        using var process = Process.Start(psi);
    }
}
