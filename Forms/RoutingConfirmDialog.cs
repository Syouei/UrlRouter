using System.Runtime.InteropServices;
using System.Windows.Forms;
using UrlRouter.Engine;
using UrlRouter.Models;

namespace UrlRouter.Forms;

internal class RoutingConfirmDialog : Form
{
    private readonly TextBox _txtUrl;
    private readonly ComboBox _cmbBrowser;
    private readonly Label _lblMatchedRule;
    private readonly CheckBox _chkRemember;
    private readonly TextBox _txtRememberName;
    private System.Windows.Forms.Timer? _copyFeedbackTimer;
    private readonly Button _btnOpen;
    private readonly Button _btnCopy;
    private readonly Button _btnCancel;

    private readonly MatchResult _match;
    private readonly BrowserTarget _defaultBrowser;

    public string FinalUrl => _txtUrl.Text.Trim();
    public string SelectedBrowserExe { get; private set; } = "";

    public RoutingConfirmDialog(string url, MatchResult match, BrowserTarget defaultBrowser)
    {
        _match = match;
        _defaultBrowser = defaultBrowser;

        Text = "About to open link";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(700, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        ShowInTaskbar = false;

        var margin = 12;
        var rowWidth = ClientSize.Width - margin * 2;

        var lbl = new Label
        {
            Left = margin, Top = 14, Width = rowWidth,
            Text = "An app requested to open the following link:"
        };

        _txtUrl = new TextBox
        {
            Left = margin, Top = 40, Width = rowWidth,
            Text = url, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _lblMatchedRule = new Label
        {
            Left = margin, Top = 68, Width = rowWidth,
            Text = match.MatchedRule != null
                ? $"Matched rule: {match.MatchedRule.Name} ({match.MatchedRule.TimeCondition?.Summary ?? "Always"})"
                : "(Default browser — no rule matched)"
        };

        var browserLabel = new Label
        {
            Left = margin, Top = 96, Width = 80, Height = 20,
            Text = "Browser:"
        };

        _cmbBrowser = new ComboBox
        {
            Left = margin + 82, Top = 94, Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbBrowser.Items.AddRange(new object[] { "Edge", "Chrome", "Firefox", "Custom" });
        _cmbBrowser.SelectedIndex = GetSelectedBrowserIndex();
        _cmbBrowser.SelectedIndexChanged += (_, _) => UpdateSelectedExe();

        var openWithLabel = new Label
        {
            Left = margin + 315, Top = 96, Width = 280, Height = 20,
            Text = match.BrowserExeResolved
                ? $"Opening in: {Path.GetFileNameWithoutExtension(match.BrowserExe)}"
                : "Browser not found on this system",
            ForeColor = match.BrowserExeResolved ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed
        };

        _chkRemember = new CheckBox
        {
            Left = margin, Top = 128, Width = rowWidth,
            Text = "Remember this choice (create a rule for this domain)"
        };
        _chkRemember.CheckedChanged += (_, _) =>
        {
            _txtRememberName!.Visible = _chkRemember.Checked;
        };

        _txtRememberName = new TextBox
        {
            Left = margin + 20, Top = 152, Width = rowWidth - 20,
            Text = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : "new-rule",
            Visible = false
        };

        int btnY = 200;
        int btnW = 100;

        _btnOpen = new Button
        {
            Left = ClientSize.Width - margin - btnW * 3 - 20, Top = btnY,
            Width = btnW, Height = 32, Text = "Open (Enter)", TabIndex = 0
        };
        _btnCopy = new Button
        {
            Left = ClientSize.Width - margin - btnW * 2 - 10, Top = btnY,
            Width = btnW, Height = 32, Text = "Copy"
        };
        _btnCancel = new Button
        {
            Left = ClientSize.Width - margin - btnW, Top = btnY,
            Width = btnW, Height = 32, Text = "Cancel (Esc)", TabIndex = 1
        };

        _btnOpen.Click += BtnOpen_Click;
        _btnCopy.Click += BtnCopy_Click;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        AcceptButton = _btnOpen;
        CancelButton = _btnCancel;

        Controls.AddRange(new Control[] { lbl, _txtUrl, _lblMatchedRule, browserLabel, _cmbBrowser, openWithLabel,
            _chkRemember, _txtRememberName, _btnOpen, _btnCopy, _btnCancel });

        UpdateSelectedExe();
    }

    private int GetSelectedBrowserIndex()
    {
        if (_match.MatchedRule != null)
            return (int)_match.MatchedRule.Browser.Kind;
        return (int)_defaultBrowser.Kind;
    }

    private void UpdateSelectedExe()
    {
        var kind = (BrowserKind)_cmbBrowser.SelectedIndex;
        var target = new BrowserTarget { Kind = kind };
        var exe = BrowserResolver.Resolve(target);
        SelectedBrowserExe = exe ?? "";
    }

    private void BtnOpen_Click(object? sender, EventArgs e)
    {
        if (!Uri.TryCreate(_txtUrl.Text.Trim(), UriKind.Absolute, out var u) ||
            !(u.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
              u.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Invalid URL format (only http/https is supported).",
                "URL Router", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnCopy_Click(object? sender, EventArgs e)
    {
        try
        {
            Clipboard.SetText(_txtUrl.Text);
            _btnCopy.Text = "Copied!";
            _copyFeedbackTimer?.Dispose();
            _copyFeedbackTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _copyFeedbackTimer.Tick += (_, _) => { _btnCopy.Text = "Copy"; _copyFeedbackTimer.Stop(); };
            _copyFeedbackTimer.Start();
        }
        catch (ExternalException)
        {
            MessageBox.Show("Failed to access clipboard.", "URL Router",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _copyFeedbackTimer?.Dispose();
        base.Dispose(disposing);
    }
}
