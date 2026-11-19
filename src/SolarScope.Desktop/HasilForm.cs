using static SolarScope.DesktopLayout;

namespace SolarScope;

public partial class HasilForm : Form
{
    public HasilForm() => InitializeComponent();

    public HasilForm(AppController controller, SimulationResult result, Screen origin) : this()
    {
        Bind(result);
        btnEdit.Visible = btnSave.Visible = origin == Screen.Input;
        btnKembali.Visible = origin == Screen.History;
        btnEdit.Click += (_, _) => controller.Navigate(Screen.Input);
        btnKembali.Click += (_, _) => controller.Navigate(origin);
        btnSave.Click += async (_, _) => await SaveAsync(controller, result);
        SavedState(result.Estimate.SimulationId > 0);
    }

    private async Task SaveAsync(AppController controller, SimulationResult result)
    {
        if (!btnSave.Enabled || result.Estimate.SimulationId > 0) return;
        btnSave.Enabled = btnEdit.Enabled = false;
        lblStatus.Text = "Menyimpan simulasi…";
        var saved = await controller.WriteHistoryAsync(() => controller.History.InsertSimulationAsync(result.Estimate));
        if (IsDisposed) return;

        btnEdit.Enabled = true;
        if (saved.IsSuccess)
        {
            result.Estimate.SimulationId = saved.Data;
            SavedState(true);
        }
        else
        {
            btnSave.Enabled = true;
            lblStatus.Text = saved.ErrorMessage;
        }
    }

    private void Bind(SimulationResult result)
    {
        var estimate = result.Estimate;
        var summary = result.Estimate.Summary!;
        lblIdentitas.Text = $"{estimate.ScenarioName} · {estimate.Location.LocationName} · {Energy(estimate.SystemParameter.SystemCapacityKwp)} kWp";
        lblIdentitas.AccessibleDescription = lblIdentitas.Text;
        Column(dgvBulanan, "month", "Bulan");
        Column(dgvBulanan, "energy", "Estimasi energi (kWh)", numeric: true);
        foreach (var month in summary.MonthlyProductions)
            dgvBulanan.Rows.Add(month.MonthName, Energy(month.EstimatedEnergyKwh));
        SizeGrid(dgvBulanan);

        lblTotalValue.Text = Energy(summary.TotalAnnualEnergyKwh);
        lblTotalValue.AccessibleName = $"{lblTotalValue.Text} kWh";
        var highest = summary.MonthlyProductions.MaxBy(month => month.EstimatedEnergyKwh)!;
        var lowest = summary.MonthlyProductions.MinBy(month => month.EstimatedEnergyKwh)!;
        lblTertinggiValue.Text = $"{highest.MonthName} · {Energy(highest.EstimatedEnergyKwh)} kWh";
        lblTerendahValue.Text = $"{lowest.MonthName} · {Energy(lowest.EstimatedEnergyKwh)} kWh";
    }

    private static string Energy(double value) => value.ToString("N2", DemoData.Culture);

    private void SavedState(bool saved)
    {
        btnSave.Enabled = !saved;
        btnSave.Text = saved ? "Tersimpan" : "Simpan";
        lblStatus.Text = saved ? "Tersimpan di Riwayat." : "";
        lblStatus.AccessibleRole = AccessibleRole.StaticText;
    }
}
