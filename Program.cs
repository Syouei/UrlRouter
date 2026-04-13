using System.Windows.Forms;
using UrlRouter.Engine;
using UrlRouter.Forms;
using UrlRouter.Models;
using UrlRouter.Storage;
using UrlRouter.Tray;
using UrlRouter.Windows;

internal static class Program
{
    private static Mutex? _mutex;
    private static TrayManager? _trayManager;
    private static AppSettings _settings = new();
    private static string? _pendingUrl;

    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Phase 1: Single instance check
        if (!SingleInstanceHelper.IsPrimaryInstance(out _mutex))
        {
            var url = ExtractUrl(args);
            if (!string.IsNullOrWhiteSpace(url))
                SingleInstanceHelper.SendUrlToRunningInstance(url);
            return;
        }

        // Phase 2: Load settings
        _settings = SettingsStore.Load();

        // Phase 3: Start named pipe server for URL dispatch
        SingleInstanceHelper.StartPipeServer(OnUrlReceived);

        // Phase 4: Extract URL if provided at startup
        _pendingUrl = ExtractUrl(args);

        // Phase 5: Initialize tray
        _trayManager = new TrayManager(
            _settings,
            onOpenMainWindow: () => ShowMainWindow(),
            onOpenSettings: () => ShowSettings(),
            onExit: () => ExitApp());

        _trayManager.Initialize();

        // Phase 6: If a URL was provided at startup, route or show dialog based on rules
        if (!string.IsNullOrWhiteSpace(_pendingUrl))
        {
            try
            {
                var match = RuleEngine.Match(_pendingUrl, _settings);
                if (match.MatchedRule != null)
                    SilentRoute(_pendingUrl);
                else
                    ShowConfirmDialog(_pendingUrl);
            }
            catch (Exception ex)
            {
                Logger.Error("Main-StartupUrl", ex, _pendingUrl);
                ShowConfirmDialog(_pendingUrl);
            }
        }

        // Phase 7: Run message loop (tray keeps app alive)
        Application.Run();
    }

    private static void OnUrlReceived(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        // Capture snapshot to avoid race conditions with settings updates
        var settingsSnapshot = _settings;
        void Dispatch()
        {
            // If a rule matches, route silently regardless of ShowConfirmDialog setting
            if (settingsSnapshot.ShowConfirmDialog)
            {
                try
                {
                    var match = RuleEngine.Match(url, settingsSnapshot);
                    if (match.MatchedRule != null)
                    {
                        SilentRoute(url);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("OnUrlReceived", ex, url);
                }
                // No rule matched — show the confirm dialog
                ShowConfirmDialog(url);
            }
            else
            {
                SilentRoute(url);
            }
        }

        if (Application.OpenForms.Count > 0)
            Application.OpenForms[0]!.BeginInvoke(Dispatch);
        else
            Dispatch();
    }

    private static void ShowConfirmDialog(string url)
    {
        MatchResult match;
        try
        {
            match = RuleEngine.Match(url, _settings);
        }
        catch (Exception ex)
        {
            Logger.Error("ShowConfirmDialog", ex, url);
            MessageBox.Show($"Failed to evaluate routing rules: {ex.Message}",
                "URL Router", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var dlg = new RoutingConfirmDialog(url, match, _settings.DefaultBrowser);
        dlg.Shown += (_, _) => dlg.Activate();
        var result = dlg.ShowDialog();

        if (result != DialogResult.OK) return;

        var finalUrl = dlg.FinalUrl;
        var browserExe = dlg.SelectedBrowserExe;
        var browserKind = dlg.SelectedBrowserKind;

        if (string.IsNullOrWhiteSpace(browserExe))
        {
            MessageBox.Show("No usable browser found.", "URL Router", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Save rule if user checked "Remember this choice"
        if (dlg.RememberRule)
        {
            SaveRule(url, browserKind, dlg.RememberRuleName);
        }

        BrowserResolver.LaunchBrowser(browserExe, finalUrl);
        Logger.Open(browserExe, finalUrl);
    }

    private static void SaveRule(string url, BrowserKind browserKind, string ruleName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        var host = uri.Host.ToLowerInvariant();
        // Use exact host pattern so it matches both the bare domain and any subdomains.
        // DomainMatcher.Matches("github.com", "github.com") = true
        // DomainMatcher.Matches("www.github.com", "github.com") = true (has dot separator)
        var domainPattern = host;

        var rule = new RoutingRule
        {
            Name = string.IsNullOrWhiteSpace(ruleName) ? host : ruleName,
            DomainPattern = domainPattern,
            Browser = new BrowserTarget { Kind = browserKind },
            IsEnabled = true
        };

        _settings.Rules.Add(rule);
        _settings = SettingsStore.Load(); // reload to keep in-memory state in sync
        SettingsStore.Save(_settings);

        Logger.Log($"RULE\t{rule.Name} -> {browserKind} for pattern {domainPattern}");
    }

    private static void SilentRoute(string url)
    {
        MatchResult match;
        try
        {
            match = RuleEngine.Match(url, _settings);
        }
        catch (Exception ex)
        {
            Logger.Error("SilentRoute", ex, url);
            _trayManager?.ShowBalloon("URL Router", $"Rule evaluation failed: {ex.Message}", ToolTipIcon.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(match.BrowserExe)) return;

        BrowserResolver.LaunchBrowser(match.BrowserExe, url);
        Logger.Open(match.BrowserExe, url);

        var host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
        _trayManager?.ShowBalloon("URL Router", $"Opening {host} in {Path.GetFileNameWithoutExtension(match.BrowserExe)}");
    }

    private static void ShowMainWindow()
    {
        if (Application.OpenForms.Count == 0)
        {
            using var wnd = new MainWindow(_settings, () => _trayManager);
            wnd.ShowDialog();
            _settings = SettingsStore.Load();
        }
        else
        {
            Application.OpenForms[0]!.BringToFront();
        }
    }

    private static void ShowSettings()
    {
        using var dlg = new SettingsDialog(_settings);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _settings = SettingsStore.Load();
            _trayManager?.RefreshMenu();
        }
    }

    private static void ExitApp()
    {
        _trayManager?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        Application.Exit();
    }

    private static string ExtractUrl(string[] args)
    {
        if (args == null || args.Length == 0) return "";

        var joined = string.Join(" ", args).Trim().Trim('"');

        var idxHttp = joined.IndexOf("http://", StringComparison.OrdinalIgnoreCase);
        var idxHttps = joined.IndexOf("https://", StringComparison.OrdinalIgnoreCase);

        int idx;
        if (idxHttp < 0) idx = idxHttps;
        else if (idxHttps < 0) idx = idxHttp;
        else idx = Math.Min(idxHttp, idxHttps);

        if (idx < 0) return "";

        var url = joined[idx..].Trim().Trim('"');

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            return uri.ToString();
        }
        return "";
    }
}
