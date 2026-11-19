using static SolarScope.DesktopLayout;

namespace SolarScope;

partial class PerbandinganForm
{
    private TableLayoutPanel empty = null!;
    private Panel sourceCard = null!;
    private Label sourceName = null!;
    private Label sourceDetails = null!;
    private Button btnCreate = null!;
    private Button btnHistory = null!;
    private TextBox alternative = null!;
    private TextBox additional = null!;
    private Button btnCompare = null!;
    private DataGridView dgvComparison = null!;
    private Label lblValidation = null!;
    private Label monthlyCaption = null!;
    private TableLayoutPanel totals = null!;

    private void InitializeComponent()
    {
        btnCreate = Button("btnCreateComparisonSource", "Buat simulasi", primary: true);
        btnHistory = Button("btnOpenHistory", "Buka Riwayat");
        empty = Stack(12, Note("Buka satu hasil simulasi untuk membandingkan kapasitas."), Actions(btnCreate, btnHistory));

        sourceName = DesktopLayout.Text("", 13, true, name: "lblComparisonSource");
        sourceName.Dock = DockStyle.Top;
        sourceName.AutoSize = false;
        sourceName.AutoEllipsis = true;
        sourceName.Height = 24;
        sourceDetails = Note("");
        sourceDetails.Name = "lblComparisonDetails";
        alternative = CapacityInput("tbAlternativeCapacity", "Kapasitas alternatif (kWp)");
        additional = CapacityInput("tbAdditionalCapacity", "Kapasitas tambahan (kWp), opsional");
        btnCompare = Button("btnCompare", "Bandingkan", primary: true);
        var fields = Across(
            Stack(4, DesktopLayout.Text("Kapasitas alternatif (kWp)", 10, true), alternative),
            Stack(4, DesktopLayout.Text("Kapasitas tambahan (opsional)", 10, true), additional),
            balance: true);
        var inputRow = Across(fields, btnCompare);
        fields.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        btnCompare.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCompare.Margin = new Padding(12, 0, 0, 0);
        sourceCard = Card(Stack(10, sourceName, sourceDetails, inputRow), padding: 12);

        dgvComparison = Grid("dgvComparison", 24);
        dgvComparison.Visible = false;
        totals = new TableLayoutPanel
        {
            Name = "comparisonTotals", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top, BackColor = SolarTheme.Cream, Margin = Padding.Empty, Visible = false
        };
        lblValidation = DesktopLayout.Text("", 10, ink: Error, name: "lblValidation");
        lblValidation.Visible = false;
        lblValidation.AccessibleRole = AccessibleRole.Alert;
        monthlyCaption = DesktopLayout.Text("Estimasi bulanan (kWh)", 10.5f, true);
        monthlyCaption.Visible = false;
        Page(this, "Perbandingan", Stack(12, Title("Perbandingan"), empty, sourceCard,
            lblValidation, Stack(6, monthlyCaption, Stack(0, dgvComparison, totals))));
        AcceptButton = btnCompare;
    }

    private static TextBox CapacityInput(string name, string accessibleName) => new()
    {
        Name = name, AccessibleName = accessibleName, PlaceholderText = "kWp",
        Font = DesktopLayout.Font(11), Dock = DockStyle.Top, Margin = Padding.Empty
    };
}
