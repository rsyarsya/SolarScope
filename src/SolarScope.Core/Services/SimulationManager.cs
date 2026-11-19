using SolarScope.Core.Models;

namespace SolarScope.Core.Services;

public sealed class SimulationManager(NasaPowerClient nasaPower)
{
    private readonly ClimatologyEnergyCalculator calculator = new();

    public async Task<ServiceResult<Simulation>> CalculateAsync(
        string name, Location location, SystemParameter parameter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var simulation = new Simulation
        {
            ScenarioName = name.Trim(),
            Location = new Location
            {
                LocationName = location.LocationName.Trim(),
                Latitude = location.Latitude,
                Longitude = location.Longitude
            },
            SystemParameter = new SystemParameter
            {
                SystemCapacityKwp = parameter.SystemCapacityKwp,
                PerformanceRatio = parameter.PerformanceRatio
            }
        };

        var validation = simulation.Validate();
        if (!validation.IsValid)
        {
            return ServiceResult<Simulation>.Fail(validation.err!);
        }

        var radiation = await nasaPower.GetMonthlyRadiationAsync(simulation.Location, cancellationToken);
        if (!radiation.IsSuccess)
        {
            return ServiceResult<Simulation>.Fail(radiation.ErrorMessage!);
        }

        var estimate = calculator.Calculate(simulation.SystemParameter, radiation.Data!);
        if (!estimate.IsSuccess)
        {
            return ServiceResult<Simulation>.Fail(estimate.ErrorMessage!);
        }

        simulation.Summary = estimate.Data;
        return ServiceResult<Simulation>.Ok(simulation);
    }
}
