using static SolarScope.DesktopLayout;

namespace SolarScope;

partial class BerandaForm
{
    private Button btnCreate = null!;
    private DataGridView recentGrid = null!;
    private TableLayoutPanel recentSection = null!;

    private void InitializeComponent()
    {
        btnCreate = Button("btnCreate", "Buat simulasi", primary: true);
        recentGrid = Grid("recentGrid", 40);
        recentGrid.TabStop = false;
        Column(recentGrid, "name", "Nama simulasi");
        Column(recentGrid, "location", "Lokasi");
        Column(recentGrid, "energy", "Estimasi tahunan (kWh)", numeric: true);
        recentSection = Stack(10, DesktopLayout.Text("Simulasi terakhir", 15, true), recentGrid);
        recentSection.Visible = false;
        Page(this, "Beranda", Stack(26, Across(Title("Beranda"), btnCreate), recentSection));
    }
}
