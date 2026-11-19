namespace SolarScope;

public partial class BerandaForm : Form
{
    public BerandaForm() => InitializeComponent();

    public BerandaForm(AppController controller) : this()
    {
        btnCreate.Click += (_, _) => controller.Navigate(Screen.Input);
        foreach (var entry in controller.Session.History.Take(3))
        {
            var result = entry.Result;
            recentGrid.Rows.Add(result.Input.Name, result.Input.Location,
                result.Estimate.Summary!.TotalAnnualEnergyKwh.ToString("N2", DemoData.Culture));
        }
        DesktopLayout.SizeGrid(recentGrid, 3);
        recentSection.Visible = recentGrid.Rows.Count > 0;
    }
}
