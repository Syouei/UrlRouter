namespace UrlRouter.Engine;

internal static class DomainMatcher
{
    public static bool Matches(string host, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(host))
            return false;

        host = host.ToLowerInvariant();
        pattern = pattern.Trim().ToLowerInvariant();

        if (pattern == "*")
            return true;

        if (pattern.StartsWith("*."))
        {
            var suffix = pattern[1..];
            // Require a proper dot boundary before the suffix
            return host.EndsWith(suffix, StringComparison.Ordinal) &&
                   (host.Length == suffix.Length || host[^suffix.Length] == '.');
        }

        return host == pattern;
    }

    public static IEnumerable<string> ExampleMatches(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
            yield break;

        pattern = pattern.Trim().ToLowerInvariant();

        if (pattern.StartsWith("*."))
        {
            var suffix = pattern[1..];
            yield return "www" + suffix;
            yield return "api" + suffix;
            if (!pattern.Contains("//"))
                yield return suffix.TrimStart('.');
        }
        else
        {
            yield return pattern;
            yield return "www." + pattern;
            yield return "api." + pattern;
        }
    }
}
