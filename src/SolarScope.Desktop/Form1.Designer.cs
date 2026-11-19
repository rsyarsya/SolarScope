using static SolarScope.DesktopLayout;

namespace SolarScope;

partial class Form1
{
    private TextBox tbNamaSimulasi = null!;
    private TextBox tbLokasi = null!;
    private TextBox tbLatitude = null!;
    private TextBox tbLongitude = null!;
    private NumericUpDown nudKapasitas = null!;
    private Label errorNama = null!;
    private Label errorLokasi = null!;
    private Label errorKoordinat = null!;
    private Label errorLongitude = null!;
    private Label errorKapasitas = null!;
    private Label notice = null!;
    private Button btnLihatHasil = null!;
    private Button btnPickLocation = null!;

    private void InitializeComponent()
    {
        tbNamaSimulasi = Input("tbNamaSimulasi", "Mis. Atap rumah");
        tbLokasi = Input("tbLokasi", "Mis. Yogyakarta");
        tbLatitude = Input("tbLatitude", "Mis. −7,7956");
        tbLongitude = Input("tbLongitude", "Mis. 110,3695");
        nudKapasitas = new NumericUpDown
        {
            Name = "nudKapasitas", Minimum = 0, Maximum = 10000, Increment = 0.1m, DecimalPlaces = 2,
            Value = 3, BorderStyle = BorderStyle.None, Font = DesktopLayout.Font(11), Dock = DockStyle.Fill
        };
        errorNama = FieldError("errorNama");
        errorLokasi = FieldError("errorLokasi");
        errorKoordinat = FieldError("errorKoordinat");
        errorLongitude = FieldError("errorLongitude");
        errorKapasitas = FieldError("errorKapasitas");
        notice = Note("");
        notice.Name = "notice";
        notice.Padding = new Padding(12, 8, 12, 8);
        notice.Dock = DockStyle.Top;
        notice.Visible = false;
        notice.AccessibleRole = AccessibleRole.Alert;
        btnLihatHasil = Button("btnLihatHasil", "Hitung estimasi", primary: true);
        btnPickLocation = new MapPickerButton();
        var location = Field("Nama lo&kasi", tbLokasi, errorLokasi);
        var coordinates = Stack(4,
            Across(Field("&Latitude", tbLatitude, errorKoordinat), Field("L&ongitude", tbLongitude, errorLongitude), balance: true),
            Note("Gunakan derajat desimal. Tanda minus berarti selatan atau barat."));
        var locationFields = Stack(12, location, coordinates);
        var locationRow = Across(locationFields, btnPickLocation);
        locationFields.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnPickLocation.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnPickLocation.Margin = new Padding(14, 0, 0, 0);
        var capacity = Field("&Kapasitas sistem", nudKapasitas, errorKapasitas, "kWp");
        capacity.MaximumSize = new Size(228, 0);
        var form = Stack(14, Field("&Nama simulasi", tbNamaSimulasi, errorNama),
            locationRow,
            Stack(4, capacity, Note("Daya puncak panel surya.")),
            Rule(), notice, btnLihatHasil);
        Page(this, "Simulasi", Stack(12, Title("Simulasi"), Card(form, 672)));
        AcceptButton = btnLihatHasil;

    }
}
