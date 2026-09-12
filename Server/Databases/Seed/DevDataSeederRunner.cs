using FarmerQuest.Server.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmerQuest.Server.Databases.Seed;

/// <summary>
/// Runs all registered IDevDataSeeder instances after Migrate (Development)
/// </summary>
public static class DevDataSeederRunner
{
    // Invokes each seeder; logs and continues on individual failures so startup is not blocked
    public static async Task RunAsync(
        FarmerQuestDbContext db,
        IEnumerable<IDevDataSeeder> seeders,
        ILogger logger,
        CancellationToken ct = default)
    {
        foreach (IDevDataSeeder seeder in seeders)
        {
            try
            {
                logger.LogInformation("Dev-seed: {Name} …", seeder.Name);
                await seeder.SeedAsync(db, ct);
            }
            catch (Exception ex)
            {
                // Continue if one of the seeders fails
                logger.LogWarning(ex, "Dev-seed fejlede for {Name}", seeder.Name);
            }
        }
    }
}
