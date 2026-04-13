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
        ClientSize = new Size(540, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        int y = 12, margin = 12;
        var col2 = margin + 130;

        AddLabel("Rule name:", margin, y);
        _txtName = AddTextBox(col2, y, 390);

        y += 32;
        _chkEnabled = AddCheckBox("Enabled", margin, y);
        _chkEnabled.Checked = true;

        y += 30;
        AddLabel("Domain pattern:", margin, y);
        _txtDomain = AddTextBox(col2, y, 390);
        _txtDomain.TextChanged += (_, _) => UpdateDomainPreview();

        _lblDomainPreview = new Label
        {
            Left = col2, Top = y + 24, Width = 390, Height = 20,
            Text = "e.g. github.com  or  *.example.com", ForeColor = System.Drawing.Color.Gray
        };
        Controls.Add(_lblDomainPreview);
        y += 50;

        _chkTimeEnabled = AddCheckBox("Time restriction", margin, y);
        _chkTimeEnabled.CheckedChanged += (_, _) =>
        {
            foreach (Control c in new Control[] { _clbDays, _dtpStart, _dtpEnd })
                c.Enabled = _chkTimeEnabled.Checked;
        };

        y += 28;
        _clbDays = new CheckedListBox
        {
            Left = col2, Top = y, Width = 390, Height = 90
        };
        _clbDays.Items.AddRange(new object[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" });
        _clbDays.CheckOnClick = true;
        Controls.Add(_clbDays);

        y += 96;
        AddLabel("From:", col2, y);
        _dtpStart = new DateTimePicker
        {
            Left = col2 + 50, Top = y, Width = 100,
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true
        };
        AddLabel("To:", col2 + 170, y);
        _dtpEnd = new DateTimePicker
        {
            Left = col2 + 200, Top = y, Width = 100,
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true
        };
        Controls.AddRange(new Control[] { _dtpStart, _dtpEnd });

        y += 36;
        AddLabel("Open in:", margin, y);
        _cmbBrowser = new ComboBox
        {
            Left = col2, Top = y, Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbBrowser.Items.AddRange(new object[] { "Edge", "Chrome", "Firefox", "Custom" });
        _cmbBrowser.SelectedIndex = 0;
        _cmbBrowser.SelectedIndexChanged += (_, _) =>
        {
            _chkCustom.Visible = _cmbBrowser.SelectedIndex == 3;
            _txtCustomExe.Visible = _cmbBrowser.SelectedIndex == 3;
            _btnBrowse.Visible = _cmbBrowser.SelectedIndex == 3;
        };
        Controls.Add(_cmbBrowser);

        y += 30;
        _chkCustom = AddCheckBox("Custom executable:", margin, y);
        _chkCustom.Visible = false;
        _chkCustom.CheckedChanged += (_, _) =>
        {
            _txtCustomExe.Enabled = _chkCustom.Checked;
            _btnBrowse.Enabled = _chkCustom.Checked;
        };

        _txtCustomExe = new TextBox
        {
            Left = col2, Top = y, Width = 300, Enabled = false
        };
        _btnBrowse = new Button
        {
            Left = col2 + 290, Top = y - 1, Width = 80, Text = "Browse..."
        };
        _btnBrowse.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "*.exe|*.exe" };
            if (ofd.ShowDialog() == DialogResult.OK)
                _txtCustomExe.Text = ofd.FileName;
        };
        _txtCustomExe.Visible = false;
        _btnBrowse.Visible = false;
        Controls.AddRange(new Control[] { _txtCustomExe, _btnBrowse });

        y += 50;
        var btnOK = new Button { Text = "OK", Left = ClientSize.Width - margin - 210, Top = y, Width = 90, Height = 32 };
        var btnCancel = new Button { Text = "Cancel", Left = ClientSize.Width - margin - 100, Top = y, Width = 90, Height = 32 };
        btnOK.Click += (_, _) => { if (ValidateAndSave()) { DialogResult = DialogResult.OK; Close(); } };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.AddRange(new Control[] { btnOK, btnCancel });
    }

    private Label AddLabel(string text, int x, int y)
    {
        var l = new Label { Left = x, Top = y, Width = 120, Text = text };
        Controls.Add(l); return l;
    }

    private TextBox AddTextBox(int x, int y, int w)
    {
        var t = new TextBox { Left = x, Top = y, Width = w };
        Controls.Add(t); return t;
    }

    private CheckBox AddCheckBox(string text, int x, int y)
    {
        var c = new CheckBox { Left = x, Top = y, Text = text };
        Controls.Add(c); return c;
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
                _clbDays.SetItemChecked((int)d - 1, true);
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
