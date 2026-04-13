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

    private const int COL_NUM = 0, COL_ENABLED = 1, COL_NAME = 2, COL_DOMAIN = 3, COL_TIME = 4, COL_BROWSER = 5;

    public MainWindow(AppSettings settings, Func<TrayManager?> getTrayManager)
    {
        _settings = settings;
        _getTrayManager = getTrayManager;

        Text = "URL Router - Rule Manager";
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(700, 450);
        ClientSize = new Size(900, 540);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;

        InitControls();
        LoadRules();
    }

    private void InitControls()
    {
        // Simple vertical layout without TableLayoutPanel to avoid layout loops
        var toolbar = new FlowLayoutPanel
        {
            Left = 12, Top = 12,
            Height = 44,
            Width = ClientSize.Width - 24,
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var btnAdd    = new Button { Text = "+ Add Rule",  Width = 110, Height = 32 };
        var btnEdit   = new Button { Text = "Edit",       Width = 90,  Height = 32 };
        var btnDelete = new Button { Text = "Delete",      Width = 90,  Height = 32 };
        var btnUp     = new Button { Text = "^ Up",        Width = 70,  Height = 32 };
        var btnDown   = new Button { Text = "v Down",      Width = 70,  Height = 32 };
        var btnSep    = new Label  { Width = 20,           Height = 32  };
        var btnSet    = new Button { Text = "Settings...", Width = 100, Height = 32 };
        var btnClose  = new Button { Text = "Close",       Width = 90,  Height = 32 };

        btnAdd.Click += (_, _) => AddRule();
        btnEdit.Click += (_, _) => EditSelectedRule();
        btnDelete.Click += (_, _) => DeleteSelectedRule();
        btnUp.Click += (_, _) => MoveRule(-1);
        btnDown.Click += (_, _) => MoveRule(1);
        btnSet.Click += (_, _) => OpenSettings();
        btnClose.Click += (_, _) => Close();

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnUp, btnDown, btnSep, btnSet, btnClose });
        Controls.Add(toolbar);

        _lvRules = new ListView
        {
            Left = 12,
            Top = toolbar.Bottom + 8,
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

        _lvRules.Resize += (_, _) => RedistributeColumnWidths();
        _lvRules.DoubleClick += (_, _) => EditSelectedRule();

        Controls.Add(_lvRules);
    }

    private void RedistributeColumnWidths()
    {
        if (_lvRules.Columns.Count < 6 || _lvRules.ClientSize.Width <= 0) return;

        int totalWidth = _lvRules.ClientSize.Width;
        int fixedWidth = _lvRules.Columns[COL_NUM].Width +
                         _lvRules.Columns[COL_ENABLED].Width +
                         _lvRules.Columns[COL_BROWSER].Width;
        int flexWidth = totalWidth - fixedWidth - (6 * 2);

        int[] flexCols = { COL_NAME, COL_DOMAIN, COL_TIME };
        int flexPerCol = Math.Max(60, flexWidth / 3);
        foreach (int col in flexCols)
            _lvRules.Columns[col].Width = flexPerCol;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        // Re-position toolbar and resize listview on form resize
        if (Controls.Count >= 2)
        {
            var toolbar = Controls[0] as FlowLayoutPanel;
            var lv = Controls[1] as ListView;
            if (toolbar != null && lv != null)
            {
                toolbar.Width = ClientSize.Width - 24;
                lv.Width = ClientSize.Width - 24;
                lv.Height = ClientSize.Height - toolbar.Bottom - 50;
            }
        }
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
        using var dlg = new RuleEditorDialog(rule);
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
        settings.Rules.RemoveAll(r => r.Id == rule.Id);
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
