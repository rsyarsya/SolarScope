using SolarScope;

internal static class AppSettingsVerification
{
    private const string DbVariable = "SOLARSCOPE_DB_CONNECTION";

    public static void Run(Action<bool, string> check)
    {
        var folder = Directory.CreateTempSubdirectory("solarscope-settings-").FullName;
        var originalDb = Environment.GetEnvironmentVariable(DbVariable);
        var originalKey = Environment.GetEnvironmentVariable(AppSettings.GeoapifyEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(DbVariable, "Host=env-host;Database=postgres");
            Environment.SetEnvironmentVariable(AppSettings.GeoapifyEnvironmentVariable, "env-key");

            var fallback = AppSettings.Load(folder);
            check(fallback.DatabaseConnection == "Host=env-host;Database=postgres" && fallback.GeoapifyApiKey == "env-key",
                "Settings fall back to environment variables when the file is missing");

            File.WriteAllText(Path.Combine(folder, AppSettings.FileName), """
                { "DatabaseConnection": "Host=file-host;Database=postgres;Root Certificate=certs/root.crt", "GeoapifyApiKey": " file-key " }
                """);
            var fromFile = AppSettings.Load(folder);
            check(fromFile.GeoapifyApiKey == "file-key" && fromFile.DatabaseConnection!.Contains("file-host"),
                "Settings file overrides environment variables");
            check(fromFile.DatabaseConnection!.Contains(Path.Combine(folder, "certs", "root.crt")),
                "Relative root certificate path resolves against the application folder");

            File.WriteAllText(Path.Combine(folder, AppSettings.FileName), """{ "DatabaseConnection": "", "GeoapifyApiKey": "file-key" }""");
            var partial = AppSettings.Load(folder);
            check(partial.DatabaseConnection == "Host=env-host;Database=postgres" && partial.GeoapifyApiKey == "file-key",
                "Empty settings values fall back per field");

            File.WriteAllText(Path.Combine(folder, AppSettings.FileName), "{ broken");
            Environment.SetEnvironmentVariable(DbVariable, null);
            Environment.SetEnvironmentVariable(AppSettings.GeoapifyEnvironmentVariable, null);
            var broken = AppSettings.Load(folder);
            check(broken.DatabaseConnection is null && broken.GeoapifyApiKey is null,
                "Broken settings file is treated as not configured instead of crashing");
        }
        finally
        {
            Environment.SetEnvironmentVariable(DbVariable, originalDb);
            Environment.SetEnvironmentVariable(AppSettings.GeoapifyEnvironmentVariable, originalKey);
            Directory.Delete(folder, recursive: true);
        }
    }
}
