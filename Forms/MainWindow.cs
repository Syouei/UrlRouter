using System.Windows.Forms;
using UrlRouter.Models;
using UrlRouter.Storage;
using UrlRouter.Tray;

namespace UrlRouter.Forms;

internal class MainWindow : Form
{
    private readonly AppSettings _settings;
    private readonly Func<TrayManager?> _getTrayManager;
    private ListView _lvRules = null!;

    public MainWindow(AppSettings settings, Func<TrayManager?> getTrayManager)
    {
        _settings = settings;
        _getTrayManager = getTrayManager;

        Text = "URL Router - Rule Manager";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(800, 480);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        InitControls();
        LoadRules();
    }

    private void InitControls()
    {
        var toolbar = new FlowLayoutPanel
        {
            Left = 12, Top = 12,
            Width = ClientSize.Width - 24,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true
        };

        var btnAdd = new Button { Text = "+ Add Rule", Width = 110 };
        var btnEdit = new Button { Text = "Edit", Width = 90 };
        var btnDelete = new Button { Text = "Delete", Width = 90 };
        var btnUp = new Button { Text = "^ Up", Width = 70 };
        var btnDown = new Button { Text = "v Down", Width = 70 };
        var btnSettings = new Button { Text = "Settings...", Width = 100 };
        var btnClose = new Button { Text = "Close", Width = 90 };

        btnAdd.Click += (_, _) => AddRule();
        btnEdit.Click += (_, _) => EditSelectedRule();
        btnDelete.Click += (_, _) => DeleteSelectedRule();
        btnUp.Click += (_, _) => MoveRule(-1);
        btnDown.Click += (_, _) => MoveRule(1);
        btnSettings.Click += (_, _) => OpenSettings();
        btnClose.Click += (_, _) => Close();

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnUp, btnDown, btnSettings, btnClose });

        _lvRules = new ListView
        {
            Left = 12,
            Top = toolbar.Bottom + 10,
            Width = ClientSize.Width - 24,
            Height = ClientSize.Height - toolbar.Bottom - 50,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _lvRules.Columns.Add("#", 40);
        _lvRules.Columns.Add("Enabled", 60);
        _lvRules.Columns.Add("Name", 180);
        _lvRules.Columns.Add("Domain", 200);
        _lvRules.Columns.Add("Time", 140);
        _lvRules.Columns.Add("Browser", 100);

        _lvRules.DoubleClick += (_, _) => EditSelectedRule();

        Controls.Add(toolbar);
        Controls.Add(_lvRules);
    }

    private void LoadRules()
    {
        _lvRules.Items.Clear();
        var settings = SettingsStore.Load();
        for (int i = 0; i < settings.Rules.Count; i++)
        {
            var r = settings.Rules[i];
            var item = new ListViewItem((i + 1).ToString());
            item.SubItems.Add(r.IsEnabled ? "Yes" : "No");
            item.SubItems.Add(r.Name);
            item.SubItems.Add(r.DomainPattern);
            item.SubItems.Add(r.TimeCondition?.Summary ?? "Always");
            item.SubItems.Add(r.Browser.DisplayName);
            item.Tag = r;
            _lvRules.Items.Add(item);
        }
    }

    private void AddRule()
    {
        var rule = new RoutingRule();
        using var dlg = new RuleEditorDialog(rule);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var settings = SettingsStore.Load();
            settings.Rules.Add(dlg.Rule);
            SettingsStore.Save(settings);
            LoadRules();
        }
    }

    private void EditSelectedRule()
    {
        if (_lvRules.SelectedItems.Count == 0) return;
        var rule = (RoutingRule)_lvRules.SelectedItems[0].Tag!;
        using var dlg = new RuleEditorDialog(rule!);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var settings = SettingsStore.Load();
            var idx = settings.Rules.FindIndex(r => r.Id == rule.Id);
            if (idx >= 0)
            {
                settings.Rules[idx] = dlg.Rule;
                SettingsStore.Save(settings);
                LoadRules();
            }
        }
    }

    private void DeleteSelectedRule()
    {
        if (_lvRules.SelectedItems.Count == 0) return;
        var rule = (RoutingRule)_lvRules.SelectedItems[0].Tag!;
        var settings = SettingsStore.Load();
        settings.Rules.RemoveAll(r => r.Id == rule!.Id);
        SettingsStore.Save(settings);
        LoadRules();
    }

    private void MoveRule(int direction)
    {
        if (_lvRules.SelectedItems.Count == 0) return;
        var idx = _lvRules.SelectedItems[0].Index;
        var settings = SettingsStore.Load();
        var newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= settings.Rules.Count) return;
        (settings.Rules[idx], settings.Rules[newIdx]) = (settings.Rules[newIdx], settings.Rules[idx]);
        SettingsStore.Save(settings);
        LoadRules();
        _lvRules.Items[newIdx].Selected = true;
    }

    private void OpenSettings()
    {
        var settings = SettingsStore.Load();
        using var dlg = new SettingsDialog(settings);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _getTrayManager()?.RefreshMenu();
            LoadRules();
        }
    }
}
