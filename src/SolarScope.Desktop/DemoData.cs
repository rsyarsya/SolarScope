using System.Globalization;
using SolarScope.Core.Models;

namespace SolarScope;

public sealed record SimulationDraft(
    string Name, string Location, decimal Capacity, string Latitude = "", string Longitude = "");
public sealed record MonthlyEnergy(int Month, decimal Energy);
public sealed record SimulationResult(Guid Id, SimulationDraft Input, Simulation Estimate);
public sealed record HistoryEntry(SimulationResult Result, DateTime SavedAt);
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
    private readonly List<HistoryEntry> history = new();
    public IReadOnlyList<HistoryEntry> History => history.AsReadOnly();
    public bool[] ComparisonSelection { get; } = { true, true, true };
    public SimulationResult SetResult(SimulationDraft input, Simulation estimate) =>
        CurrentResult = new SimulationResult(Guid.NewGuid(), input, estimate);
    public bool Contains(Guid id) => history.Any(x => x.Result.Id == id);
    public bool Save(SimulationResult result)
    {
        if (Contains(result.Id)) return false;
        history.Insert(0, new HistoryEntry(result, DateTime.Now));
        return true;
    }
    public bool Delete(Guid id) => history.RemoveAll(x => x.Result.Id == id) > 0;
}
