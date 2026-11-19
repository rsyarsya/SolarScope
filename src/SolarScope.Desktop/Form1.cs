using System.Globalization;
using static SolarScope.DesktopLayout;
using SolarScope.Core.Models;
using SystemParameter = SolarScope.Core.Models.SystemParameter;

namespace SolarScope;
public partial class Form1 : Form
{
    private AppController? controller;
    private CancellationTokenSource? calculation;
    public Form1() { InitializeComponent(); }
    public Form1(AppController controller) : this()
    {
        this.controller = controller;
        tbNamaSimulasi.AccessibleName = "Nama simulasi";
        tbLokasi.AccessibleName = "Lokasi";
        nudKapasitas.AccessibleName = "Kapasitas sistem dalam kWp";
        var draft = controller.Session.Draft;
        tbNamaSimulasi.Text = draft.Name;
        tbLokasi.Text = draft.Location;
        tbLatitude.Text = draft.Latitude;
        tbLongitude.Text = draft.Longitude;
        nudKapasitas.Value = draft.Capacity;
        tbNamaSimulasi.TextChanged += (_, _) => { CaptureDraft(); errorNama.Text = ""; };
        tbLokasi.TextChanged += (_, _) => { CaptureDraft(); errorLokasi.Text = ""; };
        tbLatitude.TextChanged += (_, _) => { CaptureDraft(); errorKoordinat.Text = ""; };
        tbLongitude.TextChanged += (_, _) => { CaptureDraft(); errorLongitude.Text = ""; };
        nudKapasitas.ValueChanged += (_, _) => { CaptureDraft(); errorKapasitas.Text = ""; };
        btnLihatHasil.Click += async (_, _) => await CalculateAsync();
        FormClosed += (_, _) => calculation?.Cancel();
        Disposed += (_, _) => calculation?.Cancel();
        AcceptButton = btnLihatHasil;
    }
    private void CaptureDraft()
    {
        if (controller != null)
            controller.Session.Draft = new(tbNamaSimulasi.Text, tbLokasi.Text, nudKapasitas.Value, tbLatitude.Text, tbLongitude.Text);
    }
    private async Task CalculateAsync()
    {
        if (controller is null || calculation is not null) return;
        ShowNotice("");
        btnLihatHasil.Text = "Hitung estimasi";
        errorNama.Text = string.IsNullOrWhiteSpace(tbNamaSimulasi.Text) ? "Nama simulasi wajib diisi." : "";
        errorLokasi.Text = string.IsNullOrWhiteSpace(tbLokasi.Text) ? "Lokasi wajib diisi." : "";
        errorKapasitas.Text = nudKapasitas.Value <= 0 ? "Kapasitas harus lebih dari 0 kWp." : "";
        bool validLatitude = TryCoordinate(tbLatitude.Text, 90, out double latitude);
        bool validLongitude = TryCoordinate(tbLongitude.Text, 180, out double longitude);
        errorKoordinat.Text = validLatitude ? "" : "Masukkan Latitude antara −90 dan 90.";
        errorLongitude.Text = validLongitude ? "" : "Masukkan Longitude antara −180 dan 180.";
        if (errorNama.Text.Length > 0) { tbNamaSimulasi.Focus(); return; }
        if (errorLokasi.Text.Length > 0) { tbLokasi.Focus(); return; }
        if (!validLatitude) { tbLatitude.Focus(); return; }
        if (!validLongitude) { tbLongitude.Focus(); return; }
        if (errorKapasitas.Text.Length > 0) { nudKapasitas.Focus(); return; }
        CaptureDraft();
        var draft = controller.Session.Draft with { Name = tbNamaSimulasi.Text.Trim(), Location = tbLokasi.Text.Trim() };
        if (controller.Session.CurrentResult is { } previous && previous.Input == draft)
        {
            controller.ShowResult(previous, Screen.Input);
            return;
        }
        using var request = new CancellationTokenSource();
        calculation = request;
        SetCalculating(true);
        SimulationResult? result = null;
        try
        {
            var estimate = await controller.Simulations.CalculateAsync(draft.Name,
                new Location { LocationName = draft.Location, Latitude = latitude, Longitude = longitude },
                new SystemParameter { SystemCapacityKwp = (double)draft.Capacity }, request.Token);
            if (request.IsCancellationRequested || IsDisposed) return;
            if (estimate.IsSuccess)
                result = controller.Session.SetResult(draft, estimate.Data!);
            else
                ShowNotice(estimate.ErrorMessage ?? "Data NASA POWER gagal dimuat. Coba lagi.", error: true);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        finally
        {
            calculation = null;
            if (!IsDisposed) SetCalculating(false);
        }

        if (result is not null) controller.ShowResult(result, Screen.Input);
    }

    private void SetCalculating(bool busy)
    {
        btnLihatHasil.Enabled = tbNamaSimulasi.Enabled = tbLokasi.Enabled =
            tbLatitude.Enabled = tbLongitude.Enabled = nudKapasitas.Enabled = !busy;
        btnLihatHasil.Text = busy ? "Menghitung…" : notice.ForeColor == Error ? "Coba lagi" : "Hitung estimasi";
        if (busy) ShowNotice("Mengambil data radiasi dari NASA POWER…");
    }

    private void ShowNotice(string message, bool error = false)
    {
        notice.ForeColor = error ? Error : DarkGreen;
        notice.BackColor = error ? ErrorSurface : Soft;
        notice.Text = message;
        notice.Visible = message.Length > 0;
    }

    private static TextBox Input(string name, string placeholder) => new()
    {
        Name = name, PlaceholderText = placeholder, BorderStyle = BorderStyle.None,
        Font = DesktopLayout.Font(11), Dock = DockStyle.Fill
    };

    private static Label FieldError(string name) => new()
    {
        Name = name, AutoSize = true, ForeColor = Error, Font = DesktopLayout.Font(9.5f),
        Visible = false, AccessibleRole = AccessibleRole.Alert
    };

    private static Control Field(string title, Control editor, Label error, string? unit = null)
    {
        var caption = DesktopLayout.Text(title, 10.5f, true);
        editor.AccessibleName = title.Replace("&", "") + (unit is null ? "" : $" ({unit})");
        var border = new RoundedPanel
        {
            Height = 38, Dock = DockStyle.Top, Outline = ControlBorder, Padding = new Padding(12, 8, 12, 6)
        };
        border.Controls.Add(editor);
        if (unit is not null)
        {
            var units = Note(unit);
            units.Dock = DockStyle.Right;
            border.Controls.Add(units);
        }
        void RefreshBorder()
        {
            border.Outline = error.Text.Length > 0 ? Error : editor.ContainsFocus ? SolarTheme.Green : ControlBorder;
            border.OutlineWidth = error.Text.Length > 0 || editor.ContainsFocus ? 2 : 1;
            border.Invalidate();
        }
        editor.GotFocus += (_, _) => RefreshBorder();
        editor.LostFocus += (_, _) => RefreshBorder();
        editor.EnabledChanged += (_, _) =>
        {
            border.Fill = editor.Enabled ? Color.White : Disabled;
            editor.BackColor = border.Fill;
            border.Invalidate();
        };
        error.TextChanged += (_, _) =>
        {
            error.Visible = error.Text.Length > 0;
            RefreshBorder();
        };
        return Stack(4, caption, border, error);
    }

    private static bool TryCoordinate(string text, int limit, out double value) =>
        (double.TryParse(text, NumberStyles.Float, DemoData.Culture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        && double.IsFinite(value) && value >= -limit && value <= limit;
}
