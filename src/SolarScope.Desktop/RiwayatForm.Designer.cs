using static SolarScope.DesktopLayout;

namespace SolarScope;

partial class RiwayatForm
{
    private DataGridView dgvHistory = null!;
    private Panel emptyPanel = null!;
    private Label lblStatus = null!;
    private Button btnOpen = null!;
    private Button btnDelete = null!;
    private Button btnCreate = null!;

    private void InitializeComponent()
    {
        btnOpen = Button("btnOpen", "Buka", primary: true);
        btnDelete = Button("btnDelete", "Hapus");
        btnCreate = Button("btnCreate", "Buat simulasi");
        dgvHistory = Grid("dgvHistory", 40, select: true);
        Column(dgvHistory, "name", "Nama simulasi");
        Column(dgvHistory, "location", "Lokasi");
        Column(dgvHistory, "capacity", "Kapasitas (kWp)", numeric: true);
        Column(dgvHistory, "saved", "Waktu simpan");
        var content = Stack(12, DesktopLayout.Text("Belum ada simulasi tersimpan.", 12, true), btnCreate);
        foreach (Control child in content.Controls) child.Anchor = AnchorStyles.None;
        emptyPanel = new Panel { Dock = DockStyle.Top, Height = 188, BackColor = Color.White, Padding = new Padding(0, 54, 0, 0) };
        emptyPanel.Controls.Add(content);
        lblStatus = Note("");
        lblStatus.Visible = false;
        Page(this, "Riwayat", Stack(12, Across(Title("Riwayat"), Actions(btnOpen, btnDelete)),
            DesktopLayout.Text("Riwayat tersedia selama aplikasi dibuka.", ink: SolarTheme.MutedInk), Stack(0, dgvHistory, emptyPanel), lblStatus));
    }
}
