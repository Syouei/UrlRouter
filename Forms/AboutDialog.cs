using System.Reflection;
using System.Windows.Forms;

namespace UrlRouter.Forms;

internal class AboutDialog : Form
{
    public AboutDialog()
    {
        Text = "About URL Router";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(420, 280);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        var margin = 20;

        var iconLabel = new Label
        {
            Left = margin, Top = 20,
            Width = 64, Height = 64,
            Text = "",
        };
        // Use a text placeholder for the icon area
        var iconBg = new Label
        {
            Left = margin, Top = 20, Width = 64, Height = 64,
            BackColor = System.Drawing.Color.FromArgb(31, 115, 186),
            Text = "URL\nRouter",
            ForeColor = System.Drawing.Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
        };

        var titleLabel = new Label
        {
            Left = margin + 80, Top = 20,
            Width = ClientSize.Width - margin - 80 - margin,
            Text = "URL Router",
            Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(31, 115, 186)
        };

        var versionLabel = new Label
        {
            Left = margin + 80, Top = 50,
            Width = 300,
            Text = $"Version {version}",
            ForeColor = System.Drawing.Color.Gray
        };

        var descLabel = new Label
        {
            Left = margin, Top = 110,
            Width = ClientSize.Width - margin * 2,
            Height = 120,
            Text =
                "A smart URL router that routes links to different browsers " +
                "based on rules you define.\n\n" +
                "Supports domain patterns, time-based rules, and custom browser executables."+
                "\n\n Developer: Jieyang Wang, Chengrui Zhu"+
                "\n Website: https://github.com/HigherNut/UrlPrompt",
            ForeColor = System.Drawing.SystemColors.ControlText
        };

        var btnOK = new Button
        {
            Text = "OK",
            Left = ClientSize.Width - margin - 80,
            Top = ClientSize.Height - 50,
            Width = 80, Height = 32
        };
        btnOK.Click += (_, _) => Close();

        Controls.AddRange(new Control[] { iconBg, titleLabel, versionLabel, descLabel, btnOK });
    }
}
