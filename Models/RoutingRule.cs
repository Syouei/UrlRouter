namespace UrlRouter.Models;

public class RoutingRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Rule";
    public bool IsEnabled { get; set; } = true;
    public string DomainPattern { get; set; } = "";
    public TimeCondition? TimeCondition { get; set; }
    public BrowserTarget Browser { get; set; } = new();
}
