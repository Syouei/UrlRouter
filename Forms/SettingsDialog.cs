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
        MinimumSize = new Size(460, 420);
        ClientSize = new Size(520, 450);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;

        int margin = 16;
        int labelW = 190;
        int fieldX = margin + labelW + 8;
        int fieldW = ClientSize.Width - fieldX - margin;
        int rightBtnX = ClientSize.Width - margin - 100;

        int y = margin;

        // Default browser
        var lbl1 = new Label { Left = margin, Top = y + 3, Width = labelW, Text = "Default browser:" };
        _cmbDefaultBrowser = new ComboBox
        {
            Left = fieldX, Top = y, Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbDefaultBrowser.Items.AddRange(new object[] { "Edge", "Chrome", "Firefox", "Custom" });
        Controls.Add(lbl1);
        Controls.Add(_cmbDefaultBrowser);

        y += 36;
        _chkStartWithWindows = new CheckBox { Left = margin, Top = y, Text = "Start with Windows", Width = 400 };
        Controls.Add(_chkStartWithWindows);

        y += 28;
        _chkMinimizeToTray = new CheckBox { Left = margin, Top = y, Text = "Minimize to system tray on close", Width = 400 };
        Controls.Add(_chkMinimizeToTray);

        y += 28;
        _chkShowConfirmDialog = new CheckBox { Left = margin, Top = y, Text = "Show confirmation dialog for each URL", Width = 400 };
        Controls.Add(_chkShowConfirmDialog);

        y += 24;
        var sep = new Label { Left = margin, Top = y, Width = ClientSize.Width - margin * 2, Height = 1, BorderStyle = BorderStyle.Fixed3D };
        Controls.Add(sep);

        y += 12;
        var lbl2 = new Label { Left = margin, Top = y + 3, Width = labelW, Text = "Protocol registration:" };
        _lblRegStatus = new Label { Left = fieldX, Top = y + 3, AutoSize = true };
        Controls.Add(lbl2);
        Controls.Add(_lblRegStatus);

        y += 32;
        _btnOpenDefaults = new Button { Left = fieldX, Top = y, Width = 220, Height = 32, Text = "Open Windows Default Apps" };
        _btnOpenDefaults.Click += (_, _) => RegistrationHelper.OpenSystemDefaultApps();
        Controls.Add(_btnOpenDefaults);

        y += 38;
        _btnAbout = new Button { Left = fieldX, Top = y, Width = 100, Height = 32, Text = "About..." };
        _btnAbout.Click += (_, _) => new AboutDialog().ShowDialog();
        Controls.Add(_btnAbout);

        y += 36;
        var note = new Label
        {
            Left = margin, Top = y, Width = ClientSize.Width - margin * 2,
            Text = "Note: After registering, open Windows Settings > Default apps to set URL Router as the default HTTP/HTTPS handler.",
            ForeColor = System.Drawing.Color.Gray
        };
        Controls.Add(note);

        // Bottom buttons — fixed to form bottom
        int btnY = ClientSize.Height - margin - 32;
        int btnW = 85;
        int btnSpacing = 12;

        var btnOK = new Button
        {
            Text = "OK",
            Left = ClientSize.Width - margin - btnW,
            Top = btnY, Width = btnW, Height = 32
        };
        var btnCancel = new Button
        {
            Text = "Cancel",
            Left = btnOK.Left - btnSpacing - btnW,
            Top = btnY, Width = btnW, Height = 32
        };
        btnOK.Click += (_, _) => { SaveSettings(); DialogResult = DialogResult.OK; Close(); };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnOK);
        Controls.Add(btnCancel);
    }

    private void LoadFromSettings()
    {
        _cmbDefaultBrowser.SelectedIndex = Math.Clamp((int)_settings.DefaultBrowser.Kind, 0, 3);
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
