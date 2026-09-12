using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using FarmerQuest.Server.Configuration;

namespace FarmerQuest.Server.Databases;

/// <summary>
/// Ensures EF CLI commands (migrations/update) use the same connection resolution as runtime startup
/// </summary>
public class DbContextFactory : IDesignTimeDbContextFactory<FarmerQuestDbContext>
{
    // Builds configuration (appsettings + env), resolves SQL connection, and returns a context
    public FarmerQuestDbContext CreateDbContext(string[] args)
    {
        // Match Program.cs env loading so design-time uses .env.local overrides
        Env.Load();
        if (File.Exists(".env.local"))
            Env.Load(".env.local", new LoadOptions(setEnvVars: true, clobberExistingVars: true));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Development.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Use Development as the default environment
        var environment = new HostEnvironmentStub();
        var connectionString = DatabaseConnectionResolver.Resolve(configuration, environment);

        var optionsBuilder = new DbContextOptionsBuilder<FarmerQuestDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new FarmerQuestDbContext(optionsBuilder.Options);
    }

    // Minimal IHostEnvironment: provides the environment used to select the database connection 
    private sealed class HostEnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } =
            System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development;

        public string ApplicationName { get; set; } = "FarmerQuest.Server";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
