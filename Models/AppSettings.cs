namespace UrlRouter.Models;

public class AppSettings
{
    public List<RoutingRule> Rules { get; set; } = new();
    public BrowserTarget DefaultBrowser { get; set; } = new();
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowConfirmDialog { get; set; } = true;
}
