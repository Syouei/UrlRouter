using System.Windows.Forms;
using UrlRouter.Models;

namespace UrlRouter.Forms;

internal class RuleEditorDialog : Form
{
    private readonly RoutingRule _rule;
    public RoutingRule Rule => _rule;

    private TextBox _txtName = null!;
    private CheckBox _chkEnabled = null!;
    private TextBox _txtDomain = null!;
    private Label _lblDomainPreview = null!;
    private CheckBox _chkTimeEnabled = null!;
    private CheckedListBox _clbDays = null!;
    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;
    private ComboBox _cmbBrowser = null!;
    private CheckBox _chkCustom = null!;
    private TextBox _txtCustomExe = null!;
    private Button _btnBrowse = null!;

    public RuleEditorDialog(RoutingRule rule)
    {
        _rule = rule;
        InitializeComponent();
        LoadFromRule();
    }

    private void InitializeComponent()
    {
        Text = "Edit Rule";
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(500, 520);
        ClientSize = new Size(600, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;

        int margin = 16;
        int labelW = 130;
        int col2 = margin + labelW + 8;
        int fieldW = ClientSize.Width - col2 - margin;

        int y = margin;

        // Rule name
        var lblName = new Label { Left = margin, Top = y + 2, Width = labelW, Text = "Rule name:" };
        _txtName = new TextBox { Left = col2, Top = y, Width = fieldW };
        Controls.Add(lblName);
        Controls.Add(_txtName);

        y += 32;
        _chkEnabled = new CheckBox { Left = margin, Top = y, Text = "Enabled", Width = 200 };
        _chkEnabled.Checked = true;
        Controls.Add(_chkEnabled);

        y += 28;
        var lblDomain = new Label { Left = margin, Top = y + 2, Width = labelW, Text = "Domain pattern:" };
        _txtDomain = new TextBox { Left = col2, Top = y, Width = fieldW };
        _txtDomain.TextChanged += (_, _) => UpdateDomainPreview();
        Controls.Add(lblDomain);
        Controls.Add(_txtDomain);

        y += 28;
        _lblDomainPreview = new Label
        {
            Left = col2, Top = y + 2, Width = fieldW,
            Text = "e.g. github.com  or  *.example.com",
            ForeColor = System.Drawing.Color.Gray
        };
        Controls.Add(_lblDomainPreview);

        y += 26;
        _chkTimeEnabled = new CheckBox { Left = margin, Top = y, Text = "Time restriction", Width = 200 };
        _chkTimeEnabled.CheckedChanged += (_, _) =>
        {
            _clbDays.Enabled = _dtpStart.Enabled = _dtpEnd.Enabled = _chkTimeEnabled.Checked;
        };
        Controls.Add(_chkTimeEnabled);

        y += 26;
        _clbDays = new CheckedListBox
        {
            Left = col2, Top = y, Width = fieldW, Height = 100
        };
        _clbDays.Items.AddRange(new object[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" });
        _clbDays.CheckOnClick = true;
        Controls.Add(_clbDays);

        y += 104;
        var lblFrom = new Label { Left = col2, Top = y + 2, Width = 40, Text = "From:" };
        _dtpStart = new DateTimePicker
        {
            Left = col2 + 42, Top = y, Width = 110,
            Format = DateTimePickerFormat.Time, ShowUpDown = true
        };
        Controls.Add(lblFrom);
        Controls.Add(_dtpStart);

        y += 28;
        var lblTo = new Label { Left = col2, Top = y + 2, Width = 40, Text = "To:" };
        _dtpEnd = new DateTimePicker
        {
            Left = col2 + 42, Top = y, Width = 110,
            Format = DateTimePickerFormat.Time, ShowUpDown = true
        };
        Controls.Add(lblTo);
        Controls.Add(_dtpEnd);

        y += 32;
        var lblBrowser = new Label { Left = margin, Top = y + 2, Width = labelW, Text = "Open in:" };
        _cmbBrowser = new ComboBox
        {
            Left = col2, Top = y, Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbBrowser.Items.AddRange(new object[] { "Edge", "Chrome", "Firefox", "Custom" });
        _cmbBrowser.SelectedIndex = 0;
        _cmbBrowser.SelectedIndexChanged += (_, _) =>
        {
            bool isCustom = _cmbBrowser.SelectedIndex == 3;
            _chkCustom.Visible = isCustom;
            _txtCustomExe.Visible = isCustom;
            _btnBrowse.Visible = isCustom;
        };
        Controls.Add(lblBrowser);
        Controls.Add(_cmbBrowser);

        y += 30;
        _chkCustom = new CheckBox
        {
            Left = col2, Top = y, Text = "Custom executable:", Width = 200, Visible = false
        };
        Controls.Add(_chkCustom);

        y += 26;
        _txtCustomExe = new TextBox { Left = col2, Top = y, Width = fieldW - 96, Enabled = false, Visible = false };
        _btnBrowse = new Button
        {
            Left = col2 + fieldW - 90, Top = y - 1, Width = 86, Text = "Browse...", Visible = false
        };
        _btnBrowse.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "*.exe|*.exe" };
            if (ofd.ShowDialog() == DialogResult.OK)
                _txtCustomExe.Text = ofd.FileName;
        };
        _chkCustom.CheckedChanged += (_, _) =>
        {
            _txtCustomExe.Enabled = _btnBrowse.Enabled = _chkCustom.Checked;
        };
        Controls.Add(_txtCustomExe);
        Controls.Add(_btnBrowse);

        y += 36;
        var btnOK = new Button
        {
            Text = "OK", Width = 90, Height = 32,
            Left = ClientSize.Width - margin - 200,
            Top = ClientSize.Height - 50
        };
        var btnCancel = new Button
        {
            Text = "Cancel", Width = 90, Height = 32,
            Left = ClientSize.Width - margin - 100,
            Top = ClientSize.Height - 50
        };
        btnOK.Click += (_, _) => { if (ValidateAndSave()) { DialogResult = DialogResult.OK; Close(); } };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.AddRange(new Control[] { btnOK, btnCancel });
    }

    private void LoadFromRule()
    {
        _txtName.Text = _rule.Name;
        _chkEnabled.Checked = _rule.IsEnabled;
        _txtDomain.Text = _rule.DomainPattern;
        _cmbBrowser.SelectedIndex = (int)_rule.Browser.Kind;

        if (_rule.TimeCondition != null && !_rule.TimeCondition.IsEmpty)
        {
            _chkTimeEnabled.Checked = true;
            foreach (var d in _rule.TimeCondition.Days)
                _clbDays.SetItemChecked(((int)d + 6) % 7, true);
            if (_rule.TimeCondition.StartTime.HasValue)
                _dtpStart.Value = DateTime.Today.Add(_rule.TimeCondition.StartTime.Value.ToTimeSpan());
            if (_rule.TimeCondition.EndTime.HasValue)
                _dtpEnd.Value = DateTime.Today.Add(_rule.TimeCondition.EndTime.Value.ToTimeSpan());
        }

        if (_rule.Browser.Kind == BrowserKind.Custom)
        {
            _chkCustom.Visible = true;
            _txtCustomExe.Visible = true;
            _btnBrowse.Visible = true;
            _txtCustomExe.Text = _rule.Browser.CustomExePath ?? "";
            _chkCustom.Checked = true;
        }
    }

    private bool ValidateAndSave()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        { MessageBox.Show("Rule name is required.", "URL Router", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
        if (string.IsNullOrWhiteSpace(_txtDomain.Text))
        { MessageBox.Show("Domain pattern is required.", "URL Router", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }

        _rule.Name = _txtName.Text.Trim();
        _rule.IsEnabled = _chkEnabled.Checked;
        _rule.DomainPattern = _txtDomain.Text.Trim();
        _rule.Browser = new BrowserTarget
        {
            Kind = (BrowserKind)_cmbBrowser.SelectedIndex,
            CustomExePath = _chkCustom.Checked ? _txtCustomExe.Text.Trim() : null
        };

        if (_chkTimeEnabled.Checked)
        {
            var days = _clbDays.CheckedIndices.Cast<int>().Select(i => (DayOfWeek)(i + 1)).ToArray();
            var startTime = TimeOnly.FromDateTime(_dtpStart.Value);
            var endTime = TimeOnly.FromDateTime(_dtpEnd.Value);
            _rule.TimeCondition = new TimeCondition
            {
                Days = days,
                StartTime = startTime,
                EndTime = endTime
            };
        }
        else
        {
            _rule.TimeCondition = null;
        }

        return true;
    }

    private void UpdateDomainPreview()
    {
        var examples = Engine.DomainMatcher.ExampleMatches(_txtDomain.Text).Take(3);
        _lblDomainPreview.Text = examples.Any()
            ? $"Matches: {string.Join(", ", examples)}"
            : "e.g. github.com  or  *.example.com";
    }
}
