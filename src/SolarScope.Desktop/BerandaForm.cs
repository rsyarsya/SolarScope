namespace SolarScope;

public partial class BerandaForm : Form
{
    public BerandaForm() => InitializeComponent();

    public BerandaForm(AppController controller) : this()
    {
        btnCreate.Click += (_, _) => controller.Navigate(Screen.Input);
        Shown += async (_, _) => await LoadRecentAsync(controller);
    }

    private async Task LoadRecentAsync(AppController controller)
    {
        lblRecentStatus.Text = "Memuat simulasi terakhir…";
        lblRecentStatus.Visible = true;
        var recent = await controller.History.GetAllSimulationsAsync();
        if (IsDisposed) return;
        if (!recent.IsSuccess)
        {
            lblRecentStatus.Text = recent.ErrorMessage;
            return;
        }

        lblRecentStatus.Visible = false;
        foreach (var simulation in recent.Data!.Take(3))
        {
            recentGrid.Rows.Add(simulation.ScenarioName, simulation.Location.LocationName,
                simulation.Summary!.TotalAnnualEnergyKwh.ToString("N2", DisplayFormat.Culture));
        }
        DesktopLayout.SizeGrid(recentGrid, 3);
        recentSection.Visible = recentGrid.Rows.Count > 0;
    }
}
