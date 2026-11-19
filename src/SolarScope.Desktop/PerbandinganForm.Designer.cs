using static SolarScope.DesktopLayout;

namespace SolarScope;

partial class PerbandinganForm
{
    private CheckBox chk2 = null!;
    private CheckBox chk3 = null!;
    private CheckBox chk5 = null!;
    private Button btnCompare = null!;
    private DataGridView dgvComparison = null!;
    private Label lblSummary = null!;
    private Label lblValidation = null!;
    private TableLayoutPanel totals = null!;

    private void InitializeComponent()
    {
        chk2 = Choice("chk2", "2 kWp");
        chk3 = Choice("chk3", "3 kWp");
        chk5 = Choice("chk5", "5 kWp");
        btnCompare = Button("btnCompare", "Bandingkan", primary: true);
        var toolbar = Actions(DesktopLayout.Text("Kapasitas", 10.5f, true), chk2, chk3, chk5, btnCompare);
        foreach (Control item in toolbar.Controls) item.Anchor = AnchorStyles.None;
        dgvComparison = Grid("dgvComparison", 26);
        lblSummary = Note("");
        lblValidation = DesktopLayout.Text("", 10, ink: Error, name: "lblValidation");
        totals = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            BackColor = SolarTheme.Cream, Height = 54
        };
        var sample = DesktopLayout.Text("Data contoh", 10, true, DarkGreen);
        sample.Padding = new Padding(12, 8, 12, 8);
        sample.BackColor = SolarTheme.Cream;
        Page(this, "Perbandingan", Stack(12, Across(Title("Perbandingan"), sample),
            Card(toolbar, padding: 10), lblValidation, lblSummary, Stack(0, totals, dgvComparison)));
    }

    private static CheckBox Choice(string name, string caption) => new SolarCheckBox()
    {
        Name = name, Text = caption, AutoSize = true, Height = 36, Font = DesktopLayout.Font(10.5f, true)
    };
}
