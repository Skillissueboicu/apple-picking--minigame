using FarmerQuest.Server.Databases;
using Microsoft.EntityFrameworkCore;

namespace FarmerQuest.Server.Databases.Seed;

/// <summary>
/// TEMPLATE: copy this file when adding new game content that should live in the DB.
///
/// Steps:
/// 1. Add model + DbSet + migration (schema).
/// 2. Copy this class
/// 3. Implement SeedAsync.s idempotently (If !Any: Add).
/// 4. Register in Program.cs: <builder.Services.AddSingleton&lt;IDevDataSeeder, ShopItemDevSeeder&gt;();
///
/// Currently empty - there is no extra farm content to seed yet
/// </summary>
public sealed class FarmContentDevSeeder : IDevDataSeeder
{
    public string Name => "FarmContent";

    // No operation until farm content tables exist; keep as a registration placeholder
    public Task SeedAsync(FarmerQuestDbContext db, CancellationToken ct = default)
    {
        // Example
        /* if (!await db.ShopItems.AnyAsync(ct))
         {
             db.ShopItems.Add(new ShopItem { Name = "Gødning", Price = 50 });
             await db.SaveChangesAsync(ct);
         }*/

        return Task.CompletedTask;
    }
}
