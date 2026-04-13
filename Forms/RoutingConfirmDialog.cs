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
    public BrowserKind SelectedBrowserKind { get; private set; } = BrowserKind.Edge;
    public bool RememberRule { get; private set; } = false;
    public string RememberRuleName => _txtRememberName.Text.Trim();

    public RoutingConfirmDialog(string url, MatchResult match, BrowserTarget defaultBrowser)
    {
        _match = match;
        _defaultBrowser = defaultBrowser;

        Text = "About to open link";
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(500, 280);
        ClientSize = new Size(700, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        TopMost = true;
        ShowInTaskbar = false;

        int margin = 12;
        int rowW = ClientSize.Width - margin * 2;
        int y = margin;

        // URL description
        var lbl = new Label
        {
            Left = margin, Top = y, Width = rowW,
            Text = "An app requested to open the following link:"
        };
        Controls.Add(lbl);

        y += 24;
        _txtUrl = new TextBox
        {
            Left = margin, Top = y, Width = rowW,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(_txtUrl);

        y += 28;
        _lblMatchedRule = new Label
        {
            Left = margin, Top = y, Width = rowW,
            Text = match.MatchedRule != null
                ? $"Matched rule: {match.MatchedRule.Name} ({match.MatchedRule.TimeCondition?.Summary ?? "Always"})"
                : "(Default browser — no rule matched)"
        };
        Controls.Add(_lblMatchedRule);

        y += 26;
        var browserLbl = new Label { Left = margin, Top = y + 2, Width = 80, Text = "Browser:" };
        _cmbBrowser = new ComboBox
        {
            Left = margin + 82, Top = y, Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbBrowser.Items.AddRange(new object[] { "Edge", "Chrome", "Firefox", "Custom" });
        _cmbBrowser.SelectedIndex = GetSelectedBrowserIndex();
        _cmbBrowser.SelectedIndexChanged += (_, _) => UpdateSelectedExe();
        Controls.Add(browserLbl);
        Controls.Add(_cmbBrowser);

        y += 28;
        _chkRemember = new CheckBox
        {
            Left = margin, Top = y, Width = rowW,
            Text = "Remember this choice (create a rule for this domain)"
        };
        _chkRemember.CheckedChanged += (_, _) =>
        {
            _txtRememberName!.Visible = _chkRemember.Checked;
        };
        Controls.Add(_chkRemember);

        y += 24;
        _txtRememberName = new TextBox
        {
            Left = margin + 20, Top = y, Width = rowW - 20,
            Text = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : "new-rule",
            Visible = false
        };
        Controls.Add(_txtRememberName);

        y += 32;
        int btnY = y;
        int btnW = 110;

        _btnOpen = new Button
        {
            Text = "Open (Enter)",
            Left = ClientSize.Width - margin - btnW * 3 - 16,
            Top = btnY, Width = btnW, Height = 32,
            TabIndex = 0
        };
        _btnCopy = new Button
        {
            Text = "Copy",
            Left = ClientSize.Width - margin - btnW * 2 - 8,
            Top = btnY, Width = 90, Height = 32
        };
        _btnCancel = new Button
        {
            Text = "Cancel (Esc)",
            Left = ClientSize.Width - margin - btnW,
            Top = btnY, Width = btnW, Height = 32,
            TabIndex = 1
        };

        _btnOpen.Click += BtnOpen_Click;
        _btnCopy.Click += BtnCopy_Click;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        AcceptButton = _btnOpen;
        CancelButton = _btnCancel;

        Controls.AddRange(new Control[] { _btnOpen, _btnCopy, _btnCancel });

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
        SelectedBrowserKind = (BrowserKind)_cmbBrowser.SelectedIndex;
        var target = new BrowserTarget { Kind = SelectedBrowserKind };
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

        RememberRule = _chkRemember.Checked;
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
