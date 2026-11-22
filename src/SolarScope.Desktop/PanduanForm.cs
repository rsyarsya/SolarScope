using static SolarScope.DesktopLayout;

namespace SolarScope;

public partial class PanduanForm : Form
{
    public PanduanForm() => InitializeComponent();

    private static Label Wrap(string text, float points = 11)
    {
        var label = DesktopLayout.Text(text, points);
        label.Dock = DockStyle.Top;
        label.MaximumSize = new Size(690, 0);
        return label;
    }

    private static Panel Coordinate(string caption, string value)
    {
        var panel = new RoundedPanel { Fill = Soft, Width = 146, Height = 56, Padding = new Padding(12, 8, 12, 8) };
        panel.Controls.Add(Stack(2, Note(caption), DesktopLayout.Text(value, 12, true, DarkGreen)));
        return panel;
    }
}
