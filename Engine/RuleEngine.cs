using UrlRouter.Models;
using UrlRouter.Storage;

namespace UrlRouter.Engine;

internal static class RuleEngine
{
    public static MatchResult Match(string rawUrl, AppSettings settings)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) ||
            !(uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
              uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            return new MatchResult(null, "", false);
        }

        var host = uri.Host.ToLowerInvariant();
        var now = DateTime.Now;

        foreach (var rule in settings.Rules)
        {
            if (!rule.IsEnabled) continue;

            if (!DomainMatcher.Matches(host, rule.DomainPattern)) continue;

            if (!TimeConditionEvaluator.Matches(now, rule.TimeCondition)) continue;

            var exe = BrowserResolver.Resolve(rule.Browser);
            if (string.IsNullOrWhiteSpace(exe))
            {
                Logger.Warn("RuleEngine", $"Browser exe not found for rule: {rule.Name}");
                continue;
            }

            return new MatchResult(rule, exe, true);
        }

        var defaultExe = BrowserResolver.Resolve(settings.DefaultBrowser);
        return new MatchResult(null, defaultExe ?? "", !string.IsNullOrWhiteSpace(defaultExe));
    }

    public static MatchResult MatchForTest(string testUrl, string domainPattern, TimeCondition? timeCondition, BrowserTarget browser)
    {
        if (!Uri.TryCreate(testUrl, UriKind.Absolute, out var uri))
            return new MatchResult(null, "", false);

        var host = uri.Host.ToLowerInvariant();

        bool domainOk = DomainMatcher.Matches(host, domainPattern);
        bool timeOk = TimeConditionEvaluator.Matches(DateTime.Now, timeCondition);
        var exe = BrowserResolver.Resolve(browser);
        var resolved = !string.IsNullOrWhiteSpace(exe);

        if (!domainOk || !timeOk)
            return new MatchResult(null, exe ?? "", resolved);

        var fakeRule = new RoutingRule
        {
            DomainPattern = domainPattern,
            TimeCondition = timeCondition,
            Browser = browser,
            IsEnabled = true
        };
        return new MatchResult(fakeRule, exe ?? "", resolved);
    }
}
