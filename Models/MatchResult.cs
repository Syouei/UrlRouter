namespace UrlRouter.Models;

public record MatchResult(
    RoutingRule? MatchedRule,
    string BrowserExe,
    bool BrowserExeResolved
);
