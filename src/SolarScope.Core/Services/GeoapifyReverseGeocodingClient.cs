using System.Globalization;
using System.Text.Json;
using SolarScope.Core.Models;

namespace SolarScope.Core.Services;

public sealed class GeoapifyReverseGeocodingClient(HttpClient httpClient, string apiKey)
{
    private const string Unavailable = "Nama wilayah belum ditemukan.";

    public async Task<ServiceResult<string>> GetRegionAsync(double latitude, double longitude,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new Location { Latitude = latitude, Longitude = longitude }.Validate();
        if (!validation.IsValid) return ServiceResult<string>.Fail(validation.err!);

        var url = string.Create(CultureInfo.InvariantCulture,
            $"https://api.geoapify.com/v1/geocode/reverse?lat={latitude:R}&lon={longitude:R}&format=json&lang=id&limit=1&apiKey={Uri.EscapeDataString(apiKey)}");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return ServiceResult<string>.Fail(Unavailable);

            await using var body = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(body, cancellationToken: timeout.Token).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return ReadRegion(json.RootElement);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<string>.Fail(Unavailable);
        }
        catch (Exception error) when (error is HttpRequestException or IOException or JsonException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ServiceResult<string>.Fail(Unavailable);
        }
    }

    private static ServiceResult<string> ReadRegion(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0
            || results[0].ValueKind != JsonValueKind.Object)
        {
            return ServiceResult<string>.Fail(Unavailable);
        }

        var region = results[0];
        var district = ReadText(region, "district") ?? ReadText(region, "suburb");
        var city = ReadText(region, "city") ?? ReadText(region, "county");
        var name = (district, city) switch
        {
            (not null, not null) when !district.Equals(city, StringComparison.OrdinalIgnoreCase) => $"{district}, {city}",
            _ => district ?? city ?? ReadText(region, "state") ?? ReadText(region, "country")
        };
        return name is null ? ServiceResult<string>.Fail(Unavailable) : ServiceResult<string>.Ok(name);
    }

    private static string? ReadText(JsonElement region, string property) =>
        region.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            && value.GetString()?.Trim() is { Length: > 0 } text ? text : null;
}
