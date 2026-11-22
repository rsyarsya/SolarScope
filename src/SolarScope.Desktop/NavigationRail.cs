using static SolarScope.DesktopLayout;

namespace SolarScope;

internal sealed class NavigationRail : Panel
{
    private readonly Dictionary<Screen, Button> items = new();

    public NavigationRail(Action<Screen> navigate)
    {
        Name = "navigationRail";
        Dock = DockStyle.Left;
        Width = 184;
        Padding = new Padding(16, 20, 16, 20);
        BackColor = Rail;
        AccessibleName = "Navigasi SolarScope";

        var logo = new PictureBox
        {
            Image = SolarTheme.Logo, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(150, 50),
            AccessibleName = "SolarScope", Margin = Padding.Empty, Dock = DockStyle.Top
        };
        var top = Stack(4, Entry(Screen.Home, "Beranda"), Entry(Screen.Input, "Simulasi"),
            Entry(Screen.History, "Riwayat"), Entry(Screen.Comparison, "Perbandingan"));
        top.Padding = new Padding(0, 70, 0, 0);
        var bottom = Stack(12, Rule(), Entry(Screen.Guide, "Panduan"));
        bottom.Dock = DockStyle.Bottom;
        Controls.Add(bottom);
        Controls.Add(top);
        Controls.Add(logo);

        Button Entry(Screen screen, string label)
        {
            var button = new NavigationButton
            {
                Text = label, AccessibleName = label, Height = 40, Dock = DockStyle.Top,
                Font = DesktopLayout.Font(11), UseVisualStyleBackColor = false
            };
            button.Click += (_, _) => navigate(screen);
            items.Add(screen, button);
            return button;
        }
    }

    public void SetActive(Screen screen)
    {
        foreach (var (target, button) in items)
        {
            button.Tag = target == screen;
            button.AccessibleDescription = target == screen ? "Halaman aktif" : null;
            button.Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(SolarTheme.Border);
        e.Graphics.DrawLine(border, Width - 1, 0, Width - 1, Height);
    }

    private sealed class NavigationButton : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            var active = Tag is true;
            e.Graphics.Clear(Rail);
            if (active)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var shape = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 6);
                using var selected = new SolidBrush(SolarTheme.LightGreen);
                using var indicator = new SolidBrush(SolarTheme.Green);
                e.Graphics.FillPath(selected, shape);
                e.Graphics.FillRectangle(indicator, 8, (Height - 20) / 2, 3, 20);
            }
            var textBounds = new Rectangle(22, 0, Width - 26, Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, active ? DarkGreen : SolarTheme.Ink,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -3, -3), SolarTheme.Green,
                    active ? SolarTheme.LightGreen : Rail);
        }
    }
}
