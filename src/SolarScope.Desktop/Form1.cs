using System.Globalization;
using SolarScope.Core.Models;
using SystemParameter = SolarScope.Core.Models.SystemParameter;

namespace SolarScope;
public partial class Form1 : Form
{
    private AppController? controller;
    private CancellationTokenSource? calculation;
    public Form1() { InitializeComponent(); Text = "SolarScope | Input Simulasi"; }
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
        navHome.Click += (_, _) => controller.Navigate(Screen.Home);
        navInput.Click += (_, _) => controller?.Navigate(Screen.Input);
        navHistory.Click += (_, _) => controller?.Navigate(Screen.History);
        navComparison.Click += (_, _) => controller?.Navigate(Screen.Comparison);
        navGuide.Click += (_, _) => controller?.Navigate(Screen.Guide);
        tbNamaSimulasi.TextChanged += (_, _) => { CaptureDraft(); errorNama.Text = ""; };
        tbLokasi.TextChanged += (_, _) => { CaptureDraft(); errorLokasi.Text = ""; };
        tbLatitude.TextChanged += (_, _) => { CaptureDraft(); errorKoordinat.Text = ""; };
        tbLongitude.TextChanged += (_, _) => { CaptureDraft(); errorKoordinat.Text = ""; };
        nudKapasitas.ValueChanged += (_, _) => { CaptureDraft(); errorKapasitas.Text = ""; };
        btnLihatHasil.Click += async (_, _) => await CalculateAsync();
        btnKembali.Click += (_, _) => controller.Navigate(Screen.Home);
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
        notice.Text = "";
        errorNama.Text = string.IsNullOrWhiteSpace(tbNamaSimulasi.Text) ? "Nama simulasi wajib diisi." : "";
        errorLokasi.Text = string.IsNullOrWhiteSpace(tbLokasi.Text) ? "Lokasi wajib diisi." : "";
        errorKapasitas.Text = nudKapasitas.Value <= 0 ? "Kapasitas harus lebih dari 0 kWp." : "";
        bool validLatitude = TryCoordinate(tbLatitude.Text, 90, out double latitude);
        bool validLongitude = TryCoordinate(tbLongitude.Text, 180, out double longitude);
        errorKoordinat.Text = !validLatitude ? "Latitude harus berupa angka antara -90 dan 90."
            : !validLongitude ? "Longitude harus berupa angka antara -180 dan 180." : "";
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
                notice.Text = estimate.ErrorMessage;
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
        btnLihatHasil.Text = busy ? "Menghitung…" : "Hitung Estimasi";
        if (busy) notice.Text = "Mengambil data radiasi dari NASA POWER…";
    }

    private static bool TryCoordinate(string text, int limit, out double value) =>
        (double.TryParse(text, NumberStyles.Float, DemoData.Culture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        && double.IsFinite(value) && value >= -limit && value <= limit;
}
