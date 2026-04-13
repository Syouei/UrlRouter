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
            var suffix = pattern[1..]; // remove leading "*"
            // Host exactly equals the bare domain (github.com matches *.github.com)
            if (host.Length == suffix.Length && host.Equals(suffix, StringComparison.Ordinal))
                return true;
            // Host ends with .suffix and has a dot separator before it
            if (host.Length > suffix.Length &&
                host.EndsWith(suffix, StringComparison.Ordinal) &&
                host[^suffix.Length] == '.')
                return true;
            return false;
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
