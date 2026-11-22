using System.Globalization;
using System.Net;
using SolarScope.Core.Models;
using SolarScope.Core.Services;

internal static class NasaPowerVerification
{
    private const string Payload = """
        {
          "properties": {
            "parameter": {
              "ALLSKY_SFC_SW_DWN": {
                "DEC": 4.7568, "FEB": 4.7078, "JAN": 4.7935, "MAR": 4.9186,
                "APR": 5.0638, "MAY": 5.0258, "JUN": 4.8516, "JUL": 5.0729,
                "AUG": 5.6563, "SEP": 6.023, "OCT": 5.7655, "NOV": 5.2526, "ANN": 5.159
              }
            }
          },
          "parameters": { "ALLSKY_SFC_SW_DWN": { "units": "kW-hr/m^2/day" } }
        }
        """;

    private static Location Yogyakarta => new() { Latitude = -7.7956, Longitude = 110.3695 };

    public static async Task VerifyAsync(Action<bool, string> check)
    {
        Uri? requestedUri = null;
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            requestedUri = request.RequestUri;
            return Reply(Payload);
        }));
        var nasa = new NasaPowerClient(client);
        var latitude = await nasa.GetMonthlyRadiationAsync(new Location { Latitude = double.NaN });
        var longitude = await nasa.GetMonthlyRadiationAsync(new Location { Longitude = 181 });
        check(!latitude.IsSuccess && !longitude.IsSuccess && requestedUri is null,
            "Invalid coordinates are rejected before an HTTP request");

        var previousCulture = CultureInfo.CurrentCulture;
        ServiceResult<IReadOnlyList<double>> result;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("id-ID");
            result = await nasa.GetMonthlyRadiationAsync(Yogyakarta);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        check(requestedUri?.AbsoluteUri == "https://power.larc.nasa.gov/api/temporal/climatology/point?parameters=ALLSKY_SFC_SW_DWN&community=RE&longitude=110.3695&latitude=-7.7956&format=JSON",
            "NASA request uses the climatology endpoint and invariant coordinate formatting");
        double[] expected = [4.7935, 4.7078, 4.9186, 5.0638, 5.0258, 4.8516, 5.0729, 5.6563, 6.023, 5.7655, 5.2526, 4.7568];
        check(result.IsSuccess && result.Data!.SequenceEqual(expected), "NASA readings sort January through December");
        var estimate = new ClimatologyEnergyCalculator().Calculate(new SystemParameter { SystemCapacityKwp = 2 }, result.Data!);
        check(estimate.IsSuccess && estimate.Data!.TotalAnnualEnergyKwh == 2824.83, "NASA fixture feeds the existing calculator");

        (string Body, string Reason)[] invalidPayloads =
        [
            (Payload.Replace("kW-hr/m^2/day", "W/m^2"), "incorrect unit"),
            (Payload.Replace("\"FEB\": 4.7078,", ""), "missing month"),
            (Payload.Replace("4.7078", "-999"), "NASA fill value"),
            (Payload.Replace("4.7078", "1e400"), "non-finite number"),
            (Payload.Replace("4.7078", "\"4.7078\""), "string reading"),
            ("{", "malformed JSON"),
            ("[]", "unexpected JSON structure")
        ];
        foreach (var (body, reason) in invalidPayloads)
        {
            using var invalidClient = new HttpClient(new StubHandler((_, _) => Reply(body)));
            var invalid = await new NasaPowerClient(invalidClient).GetMonthlyRadiationAsync(Yogyakarta);
            check(!invalid.IsSuccess && invalid.ErrorMessage!.Contains("tidak valid"), $"Rejects {reason}");
        }

        using var unavailable = new HttpClient(new StubHandler((_, _) => Reply("", HttpStatusCode.ServiceUnavailable)));
        var httpError = await new NasaPowerClient(unavailable).GetMonthlyRadiationAsync(Yogyakarta);
        check(!httpError.IsSuccess && httpError.ErrorMessage!.Contains("503"), "HTTP failure returns a localized status message");

        using var disconnected = new HttpClient(new StubHandler((_, _) => throw new HttpRequestException()));
        var networkError = await new NasaPowerClient(disconnected).GetMonthlyRadiationAsync(Yogyakarta);
        check(!networkError.IsSuccess && networkError.ErrorMessage!.Contains("terhubung"), "Network failure returns a localized message");

        using var slow = new HttpClient(new StubHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        })) { Timeout = TimeSpan.FromMilliseconds(100) };
        var timeout = await new NasaPowerClient(slow).GetMonthlyRadiationAsync(Yogyakarta);
        check(!timeout.IsSuccess && timeout.ErrorMessage!.Contains("Waktu tunggu"), "Timeout returns a localized error");

        using var caller = new CancellationTokenSource();
        using var canceledClient = new HttpClient(new StubHandler((_, token) =>
        {
            caller.Cancel();
            token.ThrowIfCancellationRequested();
            return Reply(Payload);
        }));
        try
        {
            await new NasaPowerClient(canceledClient).GetMonthlyRadiationAsync(Yogyakarta, caller.Token);
            check(false, "Explicit caller cancellation propagates");
        }
        catch (OperationCanceledException) when (caller.IsCancellationRequested)
        {
            check(true, "Explicit caller cancellation propagates");
        }

        await VerifyCacheAsync(check);
        await VerifySimulationManagerAsync(check);
    }

    private static async Task VerifyCacheAsync(Action<bool, string> check)
    {
        int calls = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            calls++;
            return calls == 1 ? Reply("", HttpStatusCode.ServiceUnavailable) : Reply(Payload);
        }));
        var nasa = new NasaPowerClient(client);

        var failed = await nasa.GetMonthlyRadiationAsync(Yogyakarta);
        var retried = await nasa.GetMonthlyRadiationAsync(Yogyakarta);
        check(!failed.IsSuccess && retried.IsSuccess && calls == 2, "Failed NASA responses are not cached");

        var cached = await nasa.GetMonthlyRadiationAsync(Yogyakarta);
        check(cached.IsSuccess && cached.Data!.SequenceEqual(retried.Data!) && calls == 2,
            "Repeated coordinates reuse cached NASA readings without a new request");

        await nasa.GetMonthlyRadiationAsync(new Location { Latitude = -6.2, Longitude = 106.8 });
        check(calls == 3, "Different coordinates trigger a new NASA request");
    }

    // End-to-end facade: input -> NASA POWER (stub) -> calculator -> Simulation.
    private static async Task VerifySimulationManagerAsync(Action<bool, string> check)
    {
        int requests = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            requests++;
            return Reply(Payload);
        }));
        var manager = new SimulationManager(new NasaPowerClient(client));
        var location = new Location { Latitude = -7.7956, Longitude = 110.3695, LocationName = "  Yogyakarta " };

        var result = await manager.CalculateAsync("  Rumah ", location, new SystemParameter { SystemCapacityKwp = 2 });
        check(result.IsSuccess
              && result.Data!.ScenarioName == "Rumah"
              && result.Data.Location.LocationName == "Yogyakarta"
              && result.Data.Summary!.MonthlyProductions.Count == 12
              && result.Data.Summary.TotalAnnualEnergyKwh == 2824.83,
            "SimulationManager returns a trimmed simulation with 12 months from NASA data");
        check(!ReferenceEquals(result.Data!.Location, location), "SimulationManager copies input instead of sharing caller objects");

        requests = 0;
        var blankName = await manager.CalculateAsync(" ", location, new SystemParameter { SystemCapacityKwp = 2 });
        var zeroCapacity = await manager.CalculateAsync("Rumah", location, new SystemParameter { SystemCapacityKwp = 0 });
        check(!blankName.IsSuccess && !zeroCapacity.IsSuccess && requests == 0,
            "SimulationManager rejects invalid input before calling NASA POWER");

        using var down = new HttpClient(new StubHandler((_, _) => Reply("", HttpStatusCode.ServiceUnavailable)));
        var failed = await new SimulationManager(new NasaPowerClient(down))
            .CalculateAsync("Rumah", location, new SystemParameter { SystemCapacityKwp = 2 });
        check(!failed.IsSuccess && failed.ErrorMessage!.Contains("503"), "SimulationManager forwards NASA POWER failure as ServiceResult");
    }

    public static async Task VerifyLiveAsync(Action<bool, string> check)
    {
        using var client = new HttpClient();
        var result = await new NasaPowerClient(client).GetMonthlyRadiationAsync(Yogyakarta);
        check(result.IsSuccess, result.ErrorMessage ?? "Fetched NASA POWER readings for Yogyakarta");

        var calculator = new ClimatologyEnergyCalculator();
        var first = calculator.Calculate(new SystemParameter { SystemCapacityKwp = 2 }, result.Data!);
        var second = calculator.Calculate(new SystemParameter { SystemCapacityKwp = 4 }, result.Data!);
        check(first.IsSuccess && second.IsSuccess, "Both capacities yield estimates");

        var baseline = first.Data!;
        var doubled = second.Data!;
        foreach (var summary in new[] { baseline, doubled })
        {
            check(summary.MonthlyProductions.Count == 12, "Estimate contains 12 months");
            check(Math.Abs(summary.TotalAnnualEnergyKwh - summary.MonthlyProductions.Sum(month => month.EstimatedEnergyKwh)) < 0.005,
                "Annual estimate matches the sum of the displayed months");
        }

        // Rounding the doubled result separately can differ by one cent per month.
        check(Math.Abs(doubled.TotalAnnualEnergyKwh - 2 * baseline.TotalAnnualEnergyKwh) <= 0.12 + 1e-9,
            "Doubling the capacity doubles annual energy within rounding tolerance");
        Console.WriteLine($"Yogyakarta: 2 kWp = {baseline.TotalAnnualEnergyKwh:F2} kWh/year; 4 kWp = {doubled.TotalAnnualEnergyKwh:F2} kWh/year");
    }

    private static Task<HttpResponseMessage> Reply(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
