using System.Globalization;
using SolarScope.Core.Models;

namespace SolarScope;

public sealed record SimulationDraft(
    string Name, string Location, decimal Capacity, string Latitude = "", string Longitude = "");
public sealed record SimulationResult(SimulationDraft? Input, Simulation Estimate);

public static class DisplayFormat
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("id-ID");
}

public sealed class AppSession
{
    public SimulationDraft Draft { get; set; } = new("", "", 3);
    public SimulationResult? CurrentResult { get; private set; }
    public SimulationResult SetResult(SimulationDraft input, Simulation estimate) =>
        CurrentResult = new SimulationResult(input, estimate);

    public void ForgetResult(int simulationId)
    {
        if (CurrentResult?.Estimate.SimulationId == simulationId) CurrentResult = null;
    }
}
