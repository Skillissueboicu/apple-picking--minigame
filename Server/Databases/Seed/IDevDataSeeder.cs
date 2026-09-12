using FarmerQuest.Server.Databases;

namespace FarmerQuest.Server.Databases.Seed;

/// <summary>
/// Idempotent Development seeder: fills demo/game content after migrations.
/// Use this for content (buildings, items, …) - not schema changes.
/// </summary>
public interface IDevDataSeeder
{
    // logs name
    string Name { get; }

    // Insert missing data. Safe to call on every start: check whether data exists before Add'ing
    Task SeedAsync(FarmerQuestDbContext db, CancellationToken ct = default);
}
