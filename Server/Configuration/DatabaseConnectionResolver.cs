using Microsoft.Data.SqlClient;

namespace FarmerQuest.Server.Configuration;

/// <summary>
/// Finds a local SQL Server connection.
/// Priority: config/env. Fallback SQLEXPRESS, default instance, LocalDB
/// </summary>
public static class DatabaseConnectionResolver
{
    private const string DatabaseName = "FarmerQuest";
    private const int ConnectTimeoutSeconds = 3;

    // Picks the connection string the application should use
    public static string Resolve(IConfiguration configuration, IHostEnvironment environment, ILogger? logger = null)
    {
        // Mode: auto (try everything), express (SQLEXPRESS/default), localdb
        var mode = (
            configuration["Database:Mode"]
            ?? Environment.GetEnvironmentVariable("DATABASE__MODE")
            ?? "auto"
        ).Trim().ToLowerInvariant();

        // Connection from CONNECTIONSTRINGS__DEFAULTCONNECTION (.env.local), then appsettings
        var connectionString = GetConnectionString(configuration);

        // Fixed mode: use the connection string directly
        if (!string.IsNullOrWhiteSpace(connectionString) && mode is not "auto")
        {
            logger?.LogInformation("SQL: bruger connection string direkte (Database:Mode={Mode})", mode);
            return connectionString;
        }

        // Production/Staging: require a connection string
        if (!environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection mangler");

            return connectionString;
        }

        // Development: try directly first; fall back to auto-detect
        if (!string.IsNullOrWhiteSpace(connectionString) && mode is "auto")
        {
            if (CanConnect(connectionString, out string? directError))
            {
                logger?.LogInformation("SQL: bruger connection string direkte");
                return connectionString;
            }

            logger?.LogWarning(
                "SQL: direkte connection fejlede ({Server}): {Error} — prøver auto-detect",
                MaskServer(connectionString),
                directError);
        }

        // Find the first local connection
        foreach (var candidate in BuildCandidates(mode))
        {
            if (!CanConnect(candidate.ConnectionString))
                continue;

            logger?.LogInformation("SQL: auto-valgt {Source} ({Server})", candidate.Label, MaskServer(candidate.ConnectionString));
            return candidate.ConnectionString;
        }

        // No SQL Server found
        var machine = Environment.MachineName;
        throw new InvalidOperationException(
            $"Ingen lokal SQL Server fundet på {machine}.\n");
    }

    // Prefer .env / .env.local (CONNECTIONSTRINGS__DEFAULTCONNECTION), then appsettings
    private static string? GetConnectionString(IConfiguration configuration)
    {
        var fromEnv = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv) && !fromEnv.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            return fromEnv;

        var fromConfig = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromConfig) && !fromConfig.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            return fromConfig;

        return null;
    }

    // Builds and filters the list of local SQL instances to check
    private static IEnumerable<(string Label, string ConnectionString)> BuildCandidates(string mode)
    {
        var machine = Environment.MachineName;

        // Possible local servers in priority order
        var all = new List<(string Label, string ConnectionString)>
        {
            ($"SQLEXPRESS ({machine})", BuildConnection($"{machine}\\SQLEXPRESS")),
            ("SQLEXPRESS (localhost)", BuildConnection("localhost\\SQLEXPRESS")),
            ("SQLEXPRESS (.)", BuildConnection(".\\SQLEXPRESS")),
            ($"default ({machine})", BuildConnection(machine)),
            ("default (localhost)", BuildConnection("localhost")),
            ("default (.)", BuildConnection(".")),
            ("localdb", BuildLocalDb()),
        };

        // auto = try all
        if (mode is "auto")
            return all;

        // express = SQLEXPRESS + default instance
        if (mode is "express")
        {
            return all.Where(c => c.Label.StartsWith("SQLEXPRESS", StringComparison.OrdinalIgnoreCase)
                                  || c.Label.StartsWith("default", StringComparison.OrdinalIgnoreCase));
        }

        // e.g. localdb = only labels matching the mode name
        return all.Where(c => c.Label.StartsWith(mode, StringComparison.OrdinalIgnoreCase));
    }

    // Builds a Windows-auth connection string for given server/instance
    private static string BuildConnection(string dataSource) =>
        new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = DatabaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            ConnectTimeout = ConnectTimeoutSeconds,
        }.ConnectionString;

    // Builds a connection string for LocalDB ((localdb)\MSSQLLocalDB)
    private static string BuildLocalDb() =>
        new SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = DatabaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            ConnectTimeout = ConnectTimeoutSeconds,
        }.ConnectionString;

    // Tests whether the server is reachable (short connection to master)
    private static bool CanConnect(string connectionString) =>
        CanConnect(connectionString, out _);

    private static bool CanConnect(string connectionString, out string? error)
    {
        error = null;
        try
        {
            // Check master so it works before the FarmerQuest database exists
            var probe = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master",
                ConnectTimeout = ConnectTimeoutSeconds,
            };
            using var connection = new SqlConnection(probe.ConnectionString);
            connection.Open();
            return true;
        }
        catch (Exception ex)
        {
            // Timeout / server down / wrong instance: try next candidate
            error = ex.Message;
            return false;
        }
    }

    // Extracts only the server name for logging
    private static string MaskServer(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.DataSource;
        }
        catch
        {
            return "(ukendt)";
        }
    }
}
