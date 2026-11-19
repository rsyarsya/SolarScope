using static SolarScope.DesktopLayout;

namespace SolarScope;

partial class HasilForm
{
    private Label lblIdentitas = null!;
    private Label lblTotalValue = null!;
    private Label lblTertinggiValue = null!;
    private Label lblTerendahValue = null!;
    private Label lblStatus = null!;
    private DataGridView dgvBulanan = null!;
    private Button btnSave = null!;
    private Button btnEdit = null!;
    private Button btnKembali = null!;

    private void InitializeComponent()
    {
        lblIdentitas = Note("");
        lblIdentitas.Dock = DockStyle.Top;
        lblIdentitas.AutoSize = false;
        lblIdentitas.AutoEllipsis = true;
        lblIdentitas.Height = 20;
        lblTotalValue = DesktopLayout.Text("", 33, true, SolarTheme.Green, "lblTotalValue");
        lblTertinggiValue = DesktopLayout.Text("", 10.5f, true);
        lblTerendahValue = DesktopLayout.Text("", 10.5f, true);
        lblStatus = Note("");
        lblStatus.Name = "lblStatus";
        var source = DesktopLayout.Text("NASA POWER · klimatologi bulanan", 9.5f, true, SolarTheme.MutedInk);
        var annual = new RoundedPanel
        {
            Fill = SolarTheme.Cream, Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(20, 10, 20, 10)
        };
        var unit = DesktopLayout.Text("kWh", 14, true, DarkGreen, "lblTotalUnit");
        var value = Actions(lblTotalValue, unit);
        unit.Anchor = AnchorStyles.Bottom;
        unit.Margin = new Padding(6, 0, 0, 7);
        annual.Controls.Add(Stack(2, DesktopLayout.Text("Estimasi awal · Total tahunan", 10, true, DarkGreen), value));
        annual.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 4, BackColor = SolarTheme.Yellow });
        var stats = Across(
            Card(Across(Note("Bulan tertinggi"), lblTertinggiValue), padding: 10),
            Card(Across(Note("Bulan terendah"), lblTerendahValue), padding: 10), balance: true);
        dgvBulanan = Grid("dgvBulanan", 24);
        btnSave = Button("btnSave", "Simpan", primary: true);
        btnEdit = Button("btnEdit", "Ubah input");
        btnKembali = Button("btnKembali", "Kembali ke riwayat");
        var footer = Across(Stack(2, source, lblStatus), Actions(btnKembali, btnEdit, btnSave));
        Page(this, "Hasil", Stack(10, Stack(3, Title("Hasil"), lblIdentitas), annual, stats, dgvBulanan, footer));
    }
}
