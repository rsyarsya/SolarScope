using System.Text.Json;
using Npgsql;
using SolarScope.Core.Services;

namespace SolarScope;

// Reads solarscope.settings.json next to the executable (installer build).
// Empty or missing values fall back to environment variables (developer machines).
public sealed record AppSettings(string? DatabaseConnection, string? GeoapifyApiKey)
{
    public const string FileName = "solarscope.settings.json";
    public const string GeoapifyEnvironmentVariable = "SOLARSCOPE_GEOAPIFY_API_KEY";

    // Read on demand (small file) so changes apply without restarting the map dialog.
    public static AppSettings Current => Load(AppContext.BaseDirectory);

    public static AppSettings Load(string baseDirectory)
    {
        string? database = null, geoapify = null;
        var path = Path.Combine(baseDirectory, FileName);
        if (File.Exists(path))
        {
            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(path));
                database = ReadText(json.RootElement, nameof(DatabaseConnection));
                geoapify = ReadText(json.RootElement, nameof(GeoapifyApiKey));
            }
            catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
            {
                // Broken file behaves like a missing file. The app then shows its "not configured" messages.
            }
        }

        database ??= Environment.GetEnvironmentVariable(PostgresSimulationRepository.ConnectionEnvironmentVariable);
        geoapify ??= Environment.GetEnvironmentVariable(GeoapifyEnvironmentVariable);
        return new AppSettings(ResolveCertificate(Clean(database), baseDirectory), Clean(geoapify));
    }

    private static string? ReadText(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String ? Clean(value.GetString()) : null;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // A relative "Root Certificate" path is resolved against the application folder,
    // so the bundled certificate works wherever the app is installed.
    private static string? ResolveCertificate(string? connection, string baseDirectory)
    {
        if (connection is null) return null;
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connection);
            if (!string.IsNullOrWhiteSpace(builder.RootCertificate) && !Path.IsPathRooted(builder.RootCertificate))
            {
                builder.RootCertificate = Path.GetFullPath(Path.Combine(baseDirectory, builder.RootCertificate));
                return builder.ConnectionString;
            }
        }
        catch (ArgumentException)
        {
            // Invalid connection string is reported later by AppController.
        }
        return connection;
    }
}
