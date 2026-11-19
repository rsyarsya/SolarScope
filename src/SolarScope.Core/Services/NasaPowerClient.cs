using System.Globalization;
using System.Text.Json;
using SolarScope.Core.Models;

namespace SolarScope.Core.Services;

public sealed class NasaPowerClient(HttpClient httpClient)
{
    private const string Parameter = "ALLSKY_SFC_SW_DWN";
    private const string RadiationUnit = "kW-hr/m^2/day";
    private const string InvalidResponse = "Data radiasi dari NASA POWER tidak valid.";
    private static readonly string[] Months =
        ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"];

    public async Task<ServiceResult<IReadOnlyList<double>>> GetMonthlyRadiationAsync(
        Location location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validation = location.Validate();
        if (!validation.IsValid)
        {
            return ServiceResult<IReadOnlyList<double>>.Fail(validation.err!);
        }

        var url = string.Create(CultureInfo.InvariantCulture,
            $"https://power.larc.nasa.gov/api/temporal/climatology/point?parameters={Parameter}&community=RE&longitude={location.Longitude}&latitude={location.Latitude}&format=JSON");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));

        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<IReadOnlyList<double>>.Fail(
                    $"NASA POWER mengembalikan status HTTP {(int)response.StatusCode}. Coba lagi nanti.");
            }

            await using var body = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: timeout.Token);
            return ReadRadiation(document.RootElement);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<IReadOnlyList<double>>.Fail("Waktu tunggu NASA POWER habis. Coba lagi.");
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ServiceResult<IReadOnlyList<double>>.Fail("Tidak dapat terhubung ke NASA POWER. Periksa koneksi internet.");
        }
        catch (JsonException)
        {
            return ServiceResult<IReadOnlyList<double>>.Fail(InvalidResponse);
        }
    }

    private static ServiceResult<IReadOnlyList<double>> ReadRadiation(JsonElement root)
    {
        if (!TryGetProperty(root, "parameters", out var parameters)
            || !TryGetProperty(parameters, Parameter, out var metadata)
            || !TryGetProperty(metadata, "units", out var units)
            || units.ValueKind != JsonValueKind.String
            || !units.ValueEquals(RadiationUnit)
            || !TryGetProperty(root, "properties", out var properties)
            || !TryGetProperty(properties, "parameter", out var values)
            || !TryGetProperty(values, Parameter, out var monthly))
        {
            return ServiceResult<IReadOnlyList<double>>.Fail(InvalidResponse);
        }

        var radiation = new double[Months.Length];
        for (int index = 0; index < Months.Length; index++)
        {
            if (!TryGetProperty(monthly, Months[index], out var value)
                || value.ValueKind != JsonValueKind.Number
                || !value.TryGetDouble(out var reading)
                || !double.IsFinite(reading)
                || reading < 0)
            {
                return ServiceResult<IReadOnlyList<double>>.Fail(InvalidResponse);
            }

            radiation[index] = reading;
        }

        return ServiceResult<IReadOnlyList<double>>.Ok(Array.AsReadOnly(radiation));
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement property)
    {
        property = default;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out property);
    }
}
