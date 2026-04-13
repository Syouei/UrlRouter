using System.Diagnostics;
using Microsoft.Win32;
using UrlRouter.Storage;

namespace UrlRouter.Windows;

internal static class RegistrationHelper
{
    private const string AppKey = "UrlRouter";
    private const string HttpClass = "UrlRouter.http";
    private const string HttpsClass = "UrlRouter.https";

    private static string ExePath => Process.GetCurrentProcess().MainModule?.FileName ?? "";
    private static string ExeDir => Path.GetDirectoryName(ExePath) ?? "";

    public static bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{HttpClass}\shell\open\command");
            var val = key?.GetValue("") as string;
            return !string.IsNullOrWhiteSpace(val) && val.Contains(ExePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void Register()
    {
        try
        {
            var exePath = ExePath;
            var exeDir = ExeDir;

            Registry.CurrentUser.CreateSubKey($@"Software\RegisteredApplications")?.SetValue(AppKey, $"Software\\{AppKey}\\Capabilities");

            using var capKey = Registry.CurrentUser.CreateSubKey($@"Software\{AppKey}\Capabilities");
            capKey?.SetValue("ApplicationName", "URL Router");
            capKey?.SetValue("ApplicationDescription", "Route URLs to different browsers based on rules.");

            using var assocKey = capKey?.CreateSubKey("UrlAssociations");
            assocKey?.SetValue("http", HttpClass);
            assocKey?.SetValue("https", HttpsClass);

            RegisterProtocolClass(HttpClass, "URL Router HTTP", exePath);
            RegisterProtocolClass(HttpsClass, "URL Router HTTPS", exePath);

            using var appPathsKey = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\App Paths\{AppKey}.exe");
            appPathsKey?.SetValue("", exePath);
            appPathsKey?.SetValue("Path", exeDir);

            Logger.Log("Register\tOK");
        }
        catch (Exception ex)
        {
            Logger.Error("RegistrationHelper.Register", ex);
        }
    }

    public static void Unregister()
    {
        try
        {
            foreach (var key in new[]
            {
                $@"Software\RegisteredApplications",
                $@"Software\{AppKey}",
                $@"Software\Classes\{HttpClass}",
                $@"Software\Classes\{HttpsClass}",
                $@"Software\Microsoft\Windows\CurrentVersion\App Paths\{AppKey}.exe"
            })
            {
                try { Registry.CurrentUser.DeleteSubKeyTree(key, false); } catch { }
            }
            Logger.Log("Unregister\tOK");
        }
        catch (Exception ex)
        {
            Logger.Error("RegistrationHelper.Unregister", ex);
        }
    }

    private static void RegisterProtocolClass(string className, string description, string exePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{className}");
        key?.SetValue("", description);
        key?.SetValue("URL Protocol", "");

        using var iconKey = key?.CreateSubKey("DefaultIcon");
        iconKey?.SetValue("", $"\"{exePath}\",0");

        using var shellKey = key?.CreateSubKey("shell");
        shellKey?.SetValue("", "open");

        using var openKey = shellKey?.CreateSubKey("open");
        openKey?.SetValue("", "Open");

        using var cmdKey = openKey?.CreateSubKey("command");
        cmdKey?.SetValue("", $"\"{exePath}\" \"%1\"");
    }

    public static void OpenSystemDefaultApps()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:defaultapps",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Warn("OpenSystemDefaultApps", ex.Message);
        }
    }
}