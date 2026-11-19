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
    private Button btnRefresh = null!;

    private void InitializeComponent()
    {
        btnOpen = Button("btnOpen", "Buka", primary: true);
        btnDelete = Button("btnDelete", "Hapus");
        btnRefresh = Button("btnRefresh", "Muat ulang");
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
        emptyPanel.Visible = dgvHistory.Visible = false;
        lblStatus = Note("");
        lblStatus.Name = "lblStatus";
        lblStatus.Visible = false;
        Page(this, "Riwayat", Stack(12, Across(Title("Riwayat"), Actions(btnRefresh, btnOpen, btnDelete)),
            lblStatus, Stack(0, dgvHistory, emptyPanel)));
    }
}
