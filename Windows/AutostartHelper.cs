using System.Diagnostics;
using Microsoft.Win32;
using UrlRouter.Storage;

namespace UrlRouter.Windows;

internal static class AutostartHelper
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "UrlRouter";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            var val = key?.GetValue(AppName) as string;
            if (string.IsNullOrWhiteSpace(val)) return false;

            var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            return val.Contains(exePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void Enable()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            key?.SetValue(AppName, $"\"{exePath}\"");
            Logger.Log("Autostart\tEnable\tOK");
        }
        catch (Exception ex)
        {
            Logger.Error("AutostartHelper.Enable", ex);
        }
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            key?.DeleteValue(AppName, false);
            Logger.Log("Autostart\tDisable\tOK");
        }
        catch (Exception ex)
        {
            Logger.Error("AutostartHelper.Disable", ex);
        }
    }
}
