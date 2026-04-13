using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UrlRouter.Models;

namespace UrlRouter.Storage;

internal static class SettingsStore
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UrlRouter", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                var cfg = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (cfg != null) return cfg;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("SettingsStore.Load", ex.Message);
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            var tmp = ConfigPath + ".tmp";
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(tmp, json, Encoding.UTF8);
            File.Move(tmp, ConfigPath, overwrite: true);
        }
        catch (Exception ex)
        {
            Logger.Warn("SettingsStore.Save", ex.Message);
        }
    }
}
