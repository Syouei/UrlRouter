using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UrlRouter.Storage;

namespace UrlRouter.Tray;

internal class TrayManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private NotifyIcon? _icon;
    private readonly Models.AppSettings _settings;
    private readonly Action _onOpenMainWindow;
    private readonly Action _onOpenSettings;
    private readonly Action _onExit;
    private static Icon? _cachedIcon;

    public TrayManager(Models.AppSettings settings, Action onOpenMainWindow, Action onOpenSettings, Action onExit)
    {
        _settings = settings;
        _onOpenMainWindow = onOpenMainWindow;
        _onOpenSettings = onOpenSettings;
        _onExit = onExit;
    }

    public void Initialize()
    {
        _icon = new NotifyIcon
        {
            Text = "URL Router",
            Visible = true
        };

        _icon.DoubleClick += (_, _) => _onOpenMainWindow();
        _icon.ContextMenuStrip = CreateContextMenu();
        _icon.Icon = GetOrCreateIcon();
        _icon.ShowBalloonTip(1000, "URL Router", "Running in system tray.", ToolTipIcon.Info);
    }

    public void UpdateTooltip(string message)
    {
        if (_icon != null)
            _icon.Text = message.Length > 63 ? message[..63] : message;
    }

    public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int timeoutMs = 2000)
    {
        _icon?.ShowBalloonTip(timeoutMs, title, message, icon);
    }

    public void RefreshMenu()
    {
        if (_icon?.ContextMenuStrip != null)
        {
            _icon.ContextMenuStrip.Dispose();
            _icon.ContextMenuStrip = CreateContextMenu();
        }
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Rule Manager", null, (_, _) => _onOpenMainWindow());
        menu.Items.Add("Settings", null, (_, _) => _onOpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => _onExit());
        return menu;
    }

    private static Icon GetOrCreateIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        const int size = 16;
        using var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var bgBrush = new SolidBrush(Color.FromArgb(31, 115, 186));
        g.FillEllipse(bgBrush, 1, 1, size - 2, size - 2);

        using var pen = new Pen(Color.White, 1.2f);
        g.DrawEllipse(pen, 2, 2, size - 4, size - 4);

        using var whiteBrush = new SolidBrush(Color.White);
        g.FillEllipse(whiteBrush, 4, 5, 3, 5);
        g.FillEllipse(whiteBrush, 8, 6, 3, 5);

        var hIcon = bmp.GetHicon();
        _cachedIcon = Icon.FromHandle(hIcon);
        // The Icon now owns the handle; don't destroy it here
        return _cachedIcon;
    }

    public void Dispose()
    {
        if (_icon != null)
        {
            _icon.Visible = false;
            _icon.Dispose();
            _icon = null;
        }
        if (_cachedIcon != null)
        {
            DestroyIcon(_cachedIcon.Handle);
            _cachedIcon.Dispose();
            _cachedIcon = null;
        }
    }
}
