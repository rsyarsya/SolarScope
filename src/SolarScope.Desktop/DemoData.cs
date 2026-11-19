using System.Globalization;
using SolarScope.Core.Models;

namespace SolarScope;

public sealed record SimulationDraft(
    string Name, string Location, decimal Capacity, string Latitude = "", string Longitude = "");
public sealed record MonthlyEnergy(int Month, decimal Energy);
public sealed record SimulationResult(SimulationDraft? Input, Simulation Estimate);
public sealed record ComparisonScenario(string Name, decimal Capacity, IReadOnlyList<MonthlyEnergy> Months)
{
    public decimal AnnualTotal => Months.Sum(x => x.Energy);
}

public static class DemoData
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("id-ID");
    public static string Number(decimal value) => value.ToString("N0", Culture);
    public static string Month(int month) => Culture.DateTimeFormat.GetMonthName(month);
    private static IReadOnlyList<MonthlyEnergy> Series(params decimal[] values) =>
        Array.AsReadOnly(values.Select((v, i) => new MonthlyEnergy(i + 1, v)).ToArray());
    private static readonly IReadOnlyList<MonthlyEnergy> Sample = Series(310,295,340,355,370,365,390,410,395,380,335,320);
    // Fixture UI tetap; tidak dihitung dari input atau formula produksi energi.
    public static IReadOnlyList<ComparisonScenario> Scenarios { get; } = Array.AsReadOnly(new[] {
        new ComparisonScenario("Skenario A", 2, Series(205,195,225,235,245,240,260,275,265,250,225,210)),
        new ComparisonScenario("Skenario B", 3, Sample),
        new ComparisonScenario("Skenario C", 5, Series(520,490,570,590,615,610,650,685,660,635,560,535))
    });
    public static bool ValidComparison(int count) => count is >= 2 and <= 3;
}

public sealed class AppSession
{
    public SimulationDraft Draft { get; set; } = new("", "", 3);
    public SimulationResult? CurrentResult { get; private set; }
    public bool[] ComparisonSelection { get; } = { true, true, true };
    public SimulationResult SetResult(SimulationDraft input, Simulation estimate) =>
        CurrentResult = new SimulationResult(input, estimate);

    public void ForgetResult(int simulationId)
    {
        if (CurrentResult?.Estimate.SimulationId == simulationId) CurrentResult = null;
    }
}
