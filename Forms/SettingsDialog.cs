using System.Windows.Forms;
using UrlRouter.Models;
using UrlRouter.Storage;
using UrlRouter.Windows;

namespace UrlRouter.Forms;

internal class SettingsDialog : Form
{
    private readonly AppSettings _settings;

    private ComboBox _cmbDefaultBrowser = null!;
    private CheckBox _chkStartWithWindows = null!;
    private CheckBox _chkMinimizeToTray = null!;
    private CheckBox _chkShowConfirmDialog = null!;
    private Label _lblRegStatus = null!;
    private Button _btnOpenDefaults = null!;
    private Button _btnAbout = null!;

    public SettingsDialog(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        LoadFromSettings();
    }

    private void InitializeComponent()
    {
        Text = "Settings";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        int y = 12, margin = 12;
        int labelW = 180;

        AddLabel("Default browser:", margin, y);
        _cmbDefaultBrowser = new ComboBox
        {
            Left = margin + labelW, Top = y, Width = 250,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbDefaultBrowser.Items.AddRange(new object[] { "Edge", "Chrome", "Firefox" });
        Controls.Add(_cmbDefaultBrowser);

        y += 36;
        _chkStartWithWindows = AddCheckBox("Start with Windows", margin, y);
        y += 28;
        _chkMinimizeToTray = AddCheckBox("Minimize to system tray on close", margin, y);
        y += 28;
        _chkShowConfirmDialog = AddCheckBox("Show confirmation dialog for each URL", margin, y);
        y += 36;

        var sep = new Label
        {
            Left = margin, Top = y, Width = ClientSize.Width - margin * 2, Height = 1,
            BorderStyle = BorderStyle.Fixed3D
        };
        Controls.Add(sep);

        y += 10;
        AddLabel("Protocol registration:", margin, y);
        _lblRegStatus = new Label
        {
            Left = margin + labelW, Top = y, Width = 250,
            ForeColor = System.Drawing.Color.DarkGreen
        };
        Controls.Add(_lblRegStatus);

        y += 28;
        _btnOpenDefaults = new Button
        {
            Left = margin + labelW, Top = y, Width = 200, Height = 28,
            Text = "Open Windows Default Apps"
        };
        _btnOpenDefaults.Click += (_, _) => RegistrationHelper.OpenSystemDefaultApps();
        Controls.Add(_btnOpenDefaults);

        _btnAbout = new Button
        {
            Left = margin + labelW + 215, Top = y, Width = 90, Height = 28,
            Text = "About..."
        };
        _btnAbout.Click += (_, _) => new AboutDialog().ShowDialog();
        Controls.Add(_btnAbout);

        y += 40;
        var note = new Label
        {
            Left = margin, Top = y, Width = ClientSize.Width - margin * 2,
            Text = "Note: After registering, open Windows Settings > Default apps to set URL Router as the default HTTP/HTTPS handler.",
            ForeColor = System.Drawing.Color.Gray
        };
        Controls.Add(note);

        var btnOK = new Button
        {
            Text = "OK", Left = ClientSize.Width - margin - 190, Top = ClientSize.Height - 45,
            Width = 85, Height = 32
        };
        var btnCancel = new Button
        {
            Text = "Cancel", Left = ClientSize.Width - margin - 95, Top = ClientSize.Height - 45,
            Width = 85, Height = 32
        };
        btnOK.Click += (_, _) => { SaveSettings(); DialogResult = DialogResult.OK; Close(); };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.AddRange(new Control[] { btnOK, btnCancel });
    }

    private Label AddLabel(string text, int x, int y)
    {
        var l = new Label { Left = x, Top = y, Width = 160, Text = text };
        Controls.Add(l); return l;
    }

    private CheckBox AddCheckBox(string text, int x, int y)
    {
        var c = new CheckBox { Left = x, Top = y, Text = text, Width = 400 };
        Controls.Add(c); return c;
    }

    private void LoadFromSettings()
    {
        // Clamp to valid range to avoid -1 wraparound
        _cmbDefaultBrowser.SelectedIndex = Math.Clamp((int)_settings.DefaultBrowser.Kind, 0, 2);
        _chkStartWithWindows.Checked = _settings.StartWithWindows;
        _chkMinimizeToTray.Checked = _settings.MinimizeToTray;
        _chkShowConfirmDialog.Checked = _settings.ShowConfirmDialog;
        _lblRegStatus.Text = RegistrationHelper.IsRegistered() ? "Registered" : "Not registered";
        _lblRegStatus.ForeColor = RegistrationHelper.IsRegistered()
            ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkOrange;
    }

    private void SaveSettings()
    {
        _settings.DefaultBrowser = new BrowserTarget { Kind = (BrowserKind)_cmbDefaultBrowser.SelectedIndex };
        _settings.StartWithWindows = _chkStartWithWindows.Checked;
        _settings.MinimizeToTray = _chkMinimizeToTray.Checked;
        _settings.ShowConfirmDialog = _chkShowConfirmDialog.Checked;

        if (_settings.StartWithWindows)
            AutostartHelper.Enable();
        else
            AutostartHelper.Disable();

        SettingsStore.Save(_settings);
    }
}
