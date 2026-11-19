using SolarScope.Core.Models;

namespace SolarScope.Core.Services;

public sealed class SimulationManager(NasaPowerClient nasaPower)
{
    private readonly ClimatologyEnergyCalculator calculator = new();

    public ServiceResult<IReadOnlyList<CapacityEstimate>> CompareCapacities(
        Simulation source, IReadOnlyList<decimal> alternatives)
    {
        if (alternatives.Count is < 1 or > 2)
        {
            return ServiceResult<IReadOnlyList<CapacityEstimate>>.Fail("Masukkan satu atau dua kapasitas alternatif.");
        }

        if (alternatives.Any(value => value is < 0.01m or > 10000m || decimal.Round(value, 2) != value))
        {
            return ServiceResult<IReadOnlyList<CapacityEstimate>>.Fail(
                "Kapasitas alternatif harus antara 0,01 dan 10.000 kWp dengan maksimal dua angka desimal.");
        }

        var capacities = alternatives.Select(value => (double)value).Prepend(source.SystemParameter.SystemCapacityKwp).ToArray();
        if (capacities.Distinct().Count() != capacities.Length)
        {
            return ServiceResult<IReadOnlyList<CapacityEstimate>>.Fail("Setiap kapasitas harus berbeda.");
        }

        var months = source.Summary?.MonthlyProductions.OrderBy(month => month.MonthIndex).ToArray();
        if (months is not { Length: 12 }
            || !months.Select(month => month.MonthIndex).SequenceEqual(Enumerable.Range(1, 12))
            || months.Any(month => !double.IsFinite(month.SolarRadiationDailyAverage) || month.SolarRadiationDailyAverage < 0))
        {
            return ServiceResult<IReadOnlyList<CapacityEstimate>>.Fail("Data radiasi bulanan pada simulasi acuan tidak lengkap atau tidak valid.");
        }

        var radiation = months.Select(month => month.SolarRadiationDailyAverage).ToArray();
        var estimates = new List<CapacityEstimate>(capacities.Length);
        foreach (var capacity in capacities)
        {
            var result = calculator.Calculate(new SystemParameter
            {
                SystemCapacityKwp = capacity,
                PerformanceRatio = source.SystemParameter.PerformanceRatio
            }, radiation);
            if (!result.IsSuccess) return ServiceResult<IReadOnlyList<CapacityEstimate>>.Fail(result.ErrorMessage!);
            estimates.Add(new CapacityEstimate(capacity, result.Data!));
        }

        return ServiceResult<IReadOnlyList<CapacityEstimate>>.Ok(estimates.AsReadOnly());
    }

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
