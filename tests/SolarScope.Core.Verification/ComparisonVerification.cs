using SolarScope.Core.Models;
using SolarScope.Core.Services;

internal static class ComparisonVerification
{
    internal static void Verify(Action<bool, string> check)
    {
        using var client = new HttpClient(new UnexpectedRequest());
        var manager = new SimulationManager(new NasaPowerClient(client));
        var radiation = Enumerable.Range(1, 12).Select(month => 3.5 + month * 0.17).ToArray();
        var parameter = new SystemParameter { SystemCapacityKwp = 2, PerformanceRatio = 0.63 };
        var original = new ClimatologyEnergyCalculator().Calculate(parameter, radiation).Data!;
        var source = new Simulation { SystemParameter = parameter, Summary = original };
        var baselineMonths = original.MonthlyProductions.Select(month => month.EstimatedEnergyKwh).ToArray();
        original.MonthlyProductions.Reverse();

        var pair = manager.CompareCapacities(source, [4m]);
        check(pair.IsSuccess && pair.Data!.Select(item => item.CapacityKwp).SequenceEqual(new[] { 2d, 4d }),
            "Comparison keeps original capacity first and calculates one alternative");
        var baseline = pair.Data![0].Summary;
        var alternative = pair.Data[1].Summary;
        check(baseline.TotalAnnualEnergyKwh == original.TotalAnnualEnergyKwh
            && baseline.MonthlyProductions.Select(month => month.EstimatedEnergyKwh).SequenceEqual(baselineMonths),
            "Comparison reproduces the source total and monthly energy with the source PR");
        check(Math.Abs(alternative.TotalAnnualEnergyKwh - 2 * baseline.TotalAnnualEnergyKwh) <= 0.120001,
            "Doubling capacity doubles annual energy within monthly rounding tolerance");
        check(original.MonthlyProductions[0].MonthIndex == 12 && parameter.SystemCapacityKwp == 2,
            "Comparison sorts its own input without modifying the source");

        var three = manager.CompareCapacities(source, [4.25m, 1.5m]);
        check(three.IsSuccess && three.Data is { } items
            && items.Select(item => item.CapacityKwp).SequenceEqual(new[] { 2d, 4.25d, 1.5d })
            && items.All(item => item.Summary.MonthlyProductions.Count == 12),
            "Three scenarios retain user order and contain all twelve months");
        check(manager.CompareCapacities(source, [0.01m, 10000m]).IsSuccess, "Comparison accepts both capacity limits");
        check(!manager.CompareCapacities(source, []).IsSuccess && !manager.CompareCapacities(source, [1m, 4m, 5m]).IsSuccess,
            "Comparison accepts a total of two or three scenarios");
        check(!manager.CompareCapacities(source, [0m]).IsSuccess && !manager.CompareCapacities(source, [10000.01m]).IsSuccess
            && !manager.CompareCapacities(source, [4.123m]).IsSuccess, "Comparison rejects invalid range or precision");
        check(!manager.CompareCapacities(source, [2m]).IsSuccess && !manager.CompareCapacities(source, [4m, 4.00m]).IsSuccess,
            "Comparison rejects duplicate capacities including the source");

        var months = original.MonthlyProductions;
        months[0].SolarRadiationDailyAverage = -999;
        check(!manager.CompareCapacities(source, [4m]).IsSuccess, "Comparison rejects invalid source radiation");
        months[0].SolarRadiationDailyAverage = radiation[11];
        months[0].MonthIndex = months[1].MonthIndex;
        check(!manager.CompareCapacities(source, [4m]).IsSuccess, "Comparison rejects duplicate months");
        months.RemoveAt(0);
        check(!manager.CompareCapacities(source, [4m]).IsSuccess, "Comparison rejects incomplete monthly data");
    }

    private sealed class UnexpectedRequest : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Comparison must reuse the source data without HTTP requests.");
    }
}
