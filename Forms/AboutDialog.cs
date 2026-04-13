using System.Reflection;
using System.Windows.Forms;

namespace UrlRouter.Forms;

internal class AboutDialog : Form
{
    public AboutDialog()
    {
        Text = "About URL Router";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(550, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        int margin = 20;

        var iconBg = new Label
        {
            Left = margin, Top = margin,
            Width = 64, Height = 64,
            BackColor = System.Drawing.Color.FromArgb(31, 115, 186),
            Text = "URL\nRouter",
            ForeColor = System.Drawing.Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
        };

        var titleLbl = new Label
        {
            Left = margin + 80, Top = margin,
            Text = "URL Router",
            Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(31, 115, 186),
            AutoSize = true
        };

        var versionLbl = new Label
        {
            Left = margin + 80, Top = margin + 28,
            Text = $"Version {version}",
            ForeColor = System.Drawing.Color.Gray,
            AutoSize = true
        };

        var descLbl = new Label
        {
            Left = margin, Top = margin + 80,
            Width = ClientSize.Width - margin * 2,
            Height = 240,
            Text =
                "A smart URL router that routes links to different browsers " +
                "based on rules you define.\n\n" +
                "Supports domain patterns, time-based rules, and custom browser executables.\n\n" +
                "Developer: Jieyang Wang, Chengrui Zhu\n" +
                "Website: https://github.com/HigherNut/UrlRouter"
        };

        var btnOK = new Button
        {
            Text = "OK",
            Left = ClientSize.Width - margin - 80,
            Top = ClientSize.Height - margin - 32,
            Width = 80, Height = 32
        };
        btnOK.Click += (_, _) => Close();

        Controls.AddRange(new Control[] { iconBg, titleLbl, versionLbl, descLbl, btnOK });
    }
}
