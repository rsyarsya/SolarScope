using System.Globalization;
using SolarScope.Core.Models;
using SolarScope.Core.Services;
using static SolarScope.DesktopLayout;

namespace SolarScope;

public partial class PerbandinganForm : Form
{
    public PerbandinganForm() => InitializeComponent();

    public PerbandinganForm(AppController controller, Simulation? source) : this()
    {
        if (source is null)
        {
            sourceCard.Visible = false;
            btnCreate.Click += (_, _) => controller.Navigate(Screen.Input);
            btnHistory.Click += (_, _) => controller.Navigate(Screen.History);
            return;
        }

        empty.Visible = false;
        var location = string.IsNullOrWhiteSpace(source.Location.LocationName)
            ? $"{Form1.FormatCoordinate(source.Location.Latitude)} / {Form1.FormatCoordinate(source.Location.Longitude)}"
            : source.Location.LocationName;
        sourceName.Text = $"{source.ScenarioName} · {location}";
        var ratio = (source.SystemParameter.PerformanceRatio * 100).ToString("0.##", DisplayFormat.Culture);
        sourceDetails.Text = $"Acuan {FormatCapacity(source.SystemParameter.SystemCapacityKwp)} · PR {ratio}% · NASA POWER";
        alternative.TextChanged += (_, _) => ResetTable();
        additional.TextChanged += (_, _) => ResetTable();
        btnCompare.Click += (_, _) => Compare(controller.Simulations, source);
    }

    private void ResetTable()
    {
        dgvComparison.Columns.Clear();
        dgvComparison.Visible = totals.Visible = monthlyCaption.Visible = lblValidation.Visible = false;
        foreach (var control in totals.Controls.Cast<Control>().ToArray()) control.Dispose();
        totals.ColumnStyles.Clear();
    }

    private void Compare(SimulationManager simulations, Simulation source)
    {
        ResetTable();
        if (!TryCapacity(alternative.Text, out decimal first))
        {
            ShowError("Masukkan kapasitas alternatif dalam kWp.");
            alternative.Focus();
            return;
        }

        var values = new List<decimal> { first };
        if (!string.IsNullOrWhiteSpace(additional.Text))
        {
            if (!TryCapacity(additional.Text, out decimal second))
            {
                ShowError("Masukkan kapasitas tambahan dalam kWp atau kosongkan kolom tersebut.");
                additional.Focus();
                return;
            }
            values.Add(second);
        }

        var comparison = simulations.CompareCapacities(source, values);
        if (!comparison.IsSuccess)
        {
            ShowError(comparison.ErrorMessage!);
            return;
        }

        Bind(comparison.Data!);
    }

    private void ShowError(string message)
    {
        lblValidation.Text = message;
        lblValidation.Visible = true;
    }

    private void Bind(IReadOnlyList<CapacityEstimate> estimates)
    {
        Column(dgvComparison, "month", "Bulan");
        totals.ColumnCount = estimates.Count + 1;
        for (int i = 0; i < totals.ColumnCount; i++)
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / totals.ColumnCount));

        var caption = DesktopLayout.Text("Total tahunan (kWh)", 10, true, DarkGreen);
        caption.Margin = new Padding(12, 10, 0, 10);
        totals.Controls.Add(caption, 0, 0);
        for (int i = 0; i < estimates.Count; i++)
        {
            var estimate = estimates[i];
            var heading = FormatCapacity(estimate.CapacityKwp);
            Column(dgvComparison, $"scenario{i}", i == 0 ? $"{heading} (acuan)" : heading, numeric: true);
            var value = DesktopLayout.Text(FormatEnergy(estimate.Summary.TotalAnnualEnergyKwh), 14, true, SolarTheme.Green,
                $"lblComparisonTotal{i}");
            value.AccessibleName = $"Total tahunan {heading}: {value.Text} kWh";
            value.Anchor = AnchorStyles.Right;
            value.Margin = new Padding(0, 6, 13, 6);
            totals.Controls.Add(value, i + 1, 0);
        }

        for (int month = 0; month < 12; month++)
        {
            var cells = new List<object> { estimates[0].Summary.MonthlyProductions[month].MonthName };
            cells.AddRange(estimates.Select(estimate => (object)FormatEnergy(
                estimate.Summary.MonthlyProductions[month].EstimatedEnergyKwh)));
            dgvComparison.Rows.Add(cells.ToArray());
        }
        SizeGrid(dgvComparison);
        dgvComparison.AccessibleName = "Perbandingan estimasi energi bulanan (kWh)";
        dgvComparison.Visible = totals.Visible = monthlyCaption.Visible = true;
    }

    private static bool TryCapacity(string text, out decimal value) =>
        decimal.TryParse(text.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, DisplayFormat.Culture, out value)
        || decimal.TryParse(text.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

    private static string FormatCapacity(double value) => $"{value.ToString("0.##", DisplayFormat.Culture)} kWp";
    private static string FormatEnergy(double value) => value.ToString("N2", DisplayFormat.Culture);
}
