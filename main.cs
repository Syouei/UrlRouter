using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class UrlPrompt
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // 1) Extract URL from command line args.
        var url = ExtractUrl(args);
        if (string.IsNullOrWhiteSpace(url))
        {
            // Nothing usable was passed in, so exit quietly.
            return;
        }

        // 2) Load persisted browser preference.
        var config = AppConfig.Load();

        // 3) Ask the user to confirm or edit the URL.
        using var dlg = new UrlPromptForm(url, config);
        var result = dlg.ShowDialog();

        if (result != DialogResult.OK)
            return;

        var finalUrl = dlg.FinalUrl;

        // 4) Resolve the target browser and forward the URL.
        try
        {
            var browserExe = ResolveBrowserExe(config);
            if (string.IsNullOrWhiteSpace(browserExe))
            {
                MessageBox.Show(
                    "No usable browser is configured. Select one in the dialog or set BrowserExePath in config.",
                    "UrlPrompt",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            LaunchBrowser(browserExe, finalUrl);
            Log($"OPEN\t{browserExe}\t{finalUrl}");
        }
        catch (Exception ex)
        {
            Log($"ERROR\t{ex.GetType().Name}\t{ex.Message}\t{finalUrl}");
            MessageBox.Show(ex.ToString(), "UrlPrompt - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string ExtractUrl(string[] args)
    {
        if (args == null || args.Length == 0) return "";

        // Most callers pass the URL as the first argument, sometimes quoted.
        // Some apps split arguments, so join first, then find the URL prefix.
        var joined = string.Join(" ", args).Trim();

        // Strip wrapping quotes if present.
        joined = joined.Trim().Trim('"');

        // Find the first http(s) substring.
        var idxHttp = joined.IndexOf("http://", StringComparison.OrdinalIgnoreCase);
        var idxHttps = joined.IndexOf("https://", StringComparison.OrdinalIgnoreCase);

        int idx;
        if (idxHttp < 0) idx = idxHttps;
        else if (idxHttps < 0) idx = idxHttp;
        else idx = Math.Min(idxHttp, idxHttps);

        if (idx < 0) return "";

        var url = joined.Substring(idx).Trim();

        // Some launchers append extra trailing characters; trim conservatively.
        url = url.Trim().Trim('"');

        // Basic validation.
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            return uri.ToString();
        }

        return "";
    }

    private static void LaunchBrowser(string browserExe, string url)
    {
        var psi = new ProcessStartInfo
        {
            FileName = browserExe,
            Arguments = QuoteArg(url),
            UseShellExecute = false
        };
        Process.Start(psi);
    }

    private static string QuoteArg(string s)
    {
        // Quote/escape argument for CreateProcess when whitespace or quotes exist.
        if (string.IsNullOrEmpty(s)) return "\"\"";
        if (!s.Any(ch => char.IsWhiteSpace(ch) || ch == '"')) return s;
        return "\"" + s.Replace("\"", "\\\"") + "\"";
    }

    private static string? ResolveBrowserExe(AppConfig config)
    {
        // 1) Prefer explicit browser executable path from config.
        if (!string.IsNullOrWhiteSpace(config.BrowserExePath) && File.Exists(config.BrowserExePath))
            return config.BrowserExePath;

        // 2) Try the browser selected by the user.
        string? byChoice = config.BrowserChoice switch
        {
            BrowserChoice.Edge => FindExe("msedge.exe"),
            BrowserChoice.Chrome => FindExe("chrome.exe"),
            BrowserChoice.Firefox => FindExe("firefox.exe"),
            BrowserChoice.Custom => null,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(byChoice)) return byChoice;

        // 3) Fallback probe order.
        return FindExe("msedge.exe") ?? FindExe("chrome.exe") ?? FindExe("firefox.exe");
    }

    private static string? FindExe(string exeName)
    {
        // 1) App Paths: HKLM\Software\Microsoft\Windows\CurrentVersion\App Paths\xxx.exe
        // 2) Common install locations
        // 3) PATH
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}");
            var path = key?.GetValue("") as string;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) return path;
        }
        catch { /* ignore */ }

        var candidates = new List<string>();

        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (exeName.Equals("msedge.exe", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(Path.Combine(pf86, "Microsoft", "Edge", "Application", "msedge.exe"));
            candidates.Add(Path.Combine(pf, "Microsoft", "Edge", "Application", "msedge.exe"));
        }
        else if (exeName.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(Path.Combine(pf86, "Google", "Chrome", "Application", "chrome.exe"));
            candidates.Add(Path.Combine(pf, "Google", "Chrome", "Application", "chrome.exe"));
            candidates.Add(Path.Combine(local, "Google", "Chrome", "Application", "chrome.exe"));
        }
        else if (exeName.Equals("firefox.exe", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(Path.Combine(pf86, "Mozilla Firefox", "firefox.exe"));
            candidates.Add(Path.Combine(pf, "Mozilla Firefox", "firefox.exe"));
        }

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        // PATH
        var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in envPath.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var p = Path.Combine(dir.Trim(), exeName);
                if (File.Exists(p)) return p;
            }
            catch { /* ignore */ }
        }

        return null;
    }

    private static string ConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UrlPrompt", "config.json");

    private static string LogPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UrlPrompt", "log.txt");

    private static void Log(string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{line}{Environment.NewLine}", Encoding.UTF8);
        }
        catch { /* ignore */ }
    }

    private enum BrowserChoice { Edge, Chrome, Firefox, Custom }

    private sealed class AppConfig
    {
        public BrowserChoice BrowserChoice { get; set; } = BrowserChoice.Edge;

        // If set, this path is used first (custom/portable browser support).
        public string? BrowserExePath { get; set; }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch { /* ignore */ }
            return new AppConfig();
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json, Encoding.UTF8);
        }
    }

    private sealed class UrlPromptForm : Form
    {
        private readonly TextBox _txtUrl;
        private readonly ComboBox _cmbBrowser;
        private readonly TextBox _txtCustomExe;

        private readonly AppConfig _config;

        public string FinalUrl => _txtUrl.Text.Trim();

        public UrlPromptForm(string url, AppConfig config)
        {
            _config = config;

            const int margin = 12;
            const int contentWidth = 920;
            const int contentHeight = 280;
            const int rowWidth = contentWidth - margin * 2;
            const int buttonWidth = 112;
            const int buttonGap = 10;

            Text = "About to open link";
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(contentWidth, contentHeight);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl = new Label
            {
                Left = margin,
                Top = 14,
                Width = rowWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "An app requested to open the following link:"
            };

            _txtUrl = new TextBox
            {
                Left = margin,
                Top = 40,
                Width = rowWidth,
                Text = url,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var grp = new GroupBox
            {
                Left = margin,
                Top = 76,
                Width = rowWidth,
                Height = 94,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Target browser"
            };

            _cmbBrowser = new ComboBox
            {
                Left = 14,
                Top = 28,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbBrowser.Items.AddRange(new object[] { "Edge", "Chrome", "Firefox", "Custom (choose EXE)" });

            int customExeLeft = 222;
            int browseBtnWidth = Math.Max(118, TextRenderer.MeasureText("Browse...", Font).Width + 26);
            int browseBtnLeft = grp.Width - 14 - browseBtnWidth;
            int customExeWidth = browseBtnLeft - customExeLeft - 10;
            int browseBtnTop = _cmbBrowser.Top - 1;
            int browseBtnHeight = Math.Max(_cmbBrowser.Height + 8, TextRenderer.MeasureText("Browse...", Font).Height + 14);

            _txtCustomExe = new TextBox
            {
                Left = customExeLeft,
                Top = 28,
                Width = customExeWidth,
                Text = _config.BrowserExePath ?? "",
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var btnBrowse = new Button
            {
                Left = browseBtnLeft,
                Top = browseBtnTop,
                Width = browseBtnWidth,
                Height = browseBtnHeight,
                Text = "Browse...",
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            btnBrowse.Click += (_, __) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "Executable files (*.exe)|*.exe",
                    Title = "Select browser executable"
                };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _txtCustomExe.Text = ofd.FileName;
                }
            };

            _cmbBrowser.SelectedIndexChanged += (_, __) =>
            {
                _txtCustomExe.Enabled = (_cmbBrowser.SelectedIndex == 3);
            };

            grp.Controls.Add(_cmbBrowser);
            grp.Controls.Add(_txtCustomExe);
            grp.Controls.Add(btnBrowse);

            // Buttons
            int buttonTop = grp.Bottom + 30;
            int btnCancelLeft = ClientSize.Width - margin - buttonWidth;
            int btnCopyLeft = btnCancelLeft - buttonGap - buttonWidth;
            int btnOpenLeft = btnCopyLeft - buttonGap - buttonWidth;

            var btnOpen = new Button
            {
                Left = btnOpenLeft,
                Top = buttonTop,
                Width = buttonWidth,
                Height = 32,
                Text = "Open",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            var btnCopy = new Button
            {
                Left = btnCopyLeft,
                Top = buttonTop,
                Width = buttonWidth,
                Height = 32,
                Text = "Copy",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            var btnCancel = new Button
            {
                Left = btnCancelLeft,
                Top = buttonTop,
                Width = buttonWidth,
                Height = 32,
                Text = "Cancel",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            btnOpen.Click += (_, __) =>
            {
                if (!Uri.TryCreate(_txtUrl.Text.Trim(), UriKind.Absolute, out var u) ||
                    !(u.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) || u.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Invalid URL format (only http/https is supported).", "UrlPrompt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Persist current browser selection.
                _config.BrowserChoice = _cmbBrowser.SelectedIndex switch
                {
                    0 => BrowserChoice.Edge,
                    1 => BrowserChoice.Chrome,
                    2 => BrowserChoice.Firefox,
                    3 => BrowserChoice.Custom,
                    _ => BrowserChoice.Edge
                };
                _config.BrowserExePath = string.IsNullOrWhiteSpace(_txtCustomExe.Text) ? null : _txtCustomExe.Text.Trim();
                _config.Save();

                DialogResult = DialogResult.OK;
                Close();
            };

            btnCopy.Click += (_, __) =>
            {
                try { Clipboard.SetText(_txtUrl.Text); } catch { /* ignore */ }
            };

            btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            AcceptButton = btnOpen;
            CancelButton = btnCancel;

            Controls.Add(lbl);
            Controls.Add(_txtUrl);
            Controls.Add(grp);
            Controls.Add(btnOpen);
            Controls.Add(btnCopy);
            Controls.Add(btnCancel);

            // Initialize browser selection from config.
            _cmbBrowser.SelectedIndex = _config.BrowserChoice switch
            {
                BrowserChoice.Edge => 0,
                BrowserChoice.Chrome => 1,
                BrowserChoice.Firefox => 2,
                BrowserChoice.Custom => 3,
                _ => 0
            };
            _txtCustomExe.Enabled = (_cmbBrowser.SelectedIndex == 3);
        }
    }
}
