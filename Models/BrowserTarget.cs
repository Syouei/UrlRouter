using System.Text.Json.Serialization;

namespace UrlRouter.Models;

public class BrowserTarget
{
    public BrowserKind Kind { get; set; } = BrowserKind.Edge;

    public string? CustomExePath { get; set; }

    [JsonIgnore]
    public string DisplayName => Kind switch
    {
        BrowserKind.Edge => "Microsoft Edge",
        BrowserKind.Chrome => "Google Chrome",
        BrowserKind.Firefox => "Mozilla Firefox",
        BrowserKind.Custom => string.IsNullOrWhiteSpace(CustomExePath)
            ? "Custom (not set)"
            : Path.GetFileName(CustomExePath),
        _ => Kind.ToString()
    };
}
