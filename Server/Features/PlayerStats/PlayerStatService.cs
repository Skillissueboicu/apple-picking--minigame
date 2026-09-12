using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using FarmerQuest.Server.Databases;
using FarmerQuest.Server.Features.PlayerStats.DTOs;
using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.Features.PlayerStats;

/// <summary>
/// Player progress: badge slots, category XP, locking rules, and seeding
/// </summary>
public sealed class PlayerStatService
{
    public const int MaxPointsPerSlot = BadgeProgress.MaxPoints;

    /// <summary>
    /// All 33 badge slots (5 inner + 28 outer)
    /// </summary>
    public static readonly (string SlotKey, string Category)[] SlotDefs =
    {
        // Inner (1 per category)
        ("salgsansvarlig", "landbrugsproduktion"),
        ("fejlfinder", "teknologi"),
        ("co2_jaeger", "klimahandling"),
        ("stromspare", "energistyring"),
        ("landskabsplejer", "naturbevaring"),

        // Landbrugsproduktion — 7 outer
        ("planteekspert", "landbrugsproduktion"),
        ("saesonmester", "landbrugsproduktion"),
        ("spildkriger", "landbrugsproduktion"),
        ("jordven", "landbrugsproduktion"),
        ("drivhusgartner", "landbrugsproduktion"),
        ("hostklar", "landbrugsproduktion"),
        ("saedskiftespire", "landbrugsproduktion"),

        // Teknologi — 7 outer
        ("holdkaptajn", "teknologi"),
        ("rormester", "teknologi"),
        ("opfinderen", "teknologi"),
        ("logistikhelt", "teknologi"),
        ("vedligeholder", "teknologi"),
        ("planlaegger", "teknologi"),
        ("dokumentarist", "teknologi"),

        // Klimahandling — 6 outer
        ("jorddetektiv", "klimahandling"),
        ("vandvogter", "klimahandling"),
        ("genbrugshelt", "klimahandling"),
        ("klimatilpasser", "klimahandling"),
        ("kulstoftaenker", "klimahandling"),
        ("ren_routing", "klimahandling"),

        // Energistyring — 4 outer
        ("energiingenioer", "energistyring"),
        ("rolig_drift", "energistyring"),
        ("kort_vej", "energistyring"),
        ("smart_strom", "energistyring"),

        // Naturbevaring — 4 outer
        ("dyreven", "naturbevaring"),
        ("biodiversitetsbygger", "naturbevaring"),
        ("naturplejer", "naturbevaring"),
        ("skaansom_helt", "naturbevaring"),
    };

    // Ordered list of all badge slot keys
    public static readonly string[] BadgeKeys = SlotDefs.Select(s => s.SlotKey).ToArray();

    // Inner badge key per category (used for outer-slot locking)
    private static readonly Dictionary<string, string> InnerSlotByCategory =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["landbrugsproduktion"] = "salgsansvarlig",
            ["teknologi"] = "fejlfinder",
            ["klimahandling"] = "co2_jaeger",
            ["energistyring"] = "stromspare",
            ["naturbevaring"] = "landskabsplejer",
        };

    private static readonly HashSet<string> InnerSlotKeys =
        new(InnerSlotByCategory.Values, StringComparer.OrdinalIgnoreCase);

    private readonly FarmerQuestDbContext _db;
    private readonly ILogger<PlayerStatService> _log;
    private readonly PlayerProgressOptions _rules;

    public PlayerStatService(
        FarmerQuestDbContext db,
        ILogger<PlayerStatService> log,
        IOptions<PlayerProgressOptions> rules)
    {
        _db = db;
        _log = log;
        _rules = rules.Value;
    }

    // Loads (or creates) the caller's stats, seeds missing badges, and returns the API DTO
    public async Task<PlayerStatResponse> GetMineAsync(string userId, CancellationToken ct)
    {
        PlayerStat stat = await GetOrCreateStatAsync(userId, ct);
        await TrySeedBadgesAsync(stat, ct);
        RecalcCategoryXp(stat);
        return ToResponse(stat);
    }

    // DEBUG: set points 0–300 on one badge/slice
    public async Task<(PlayerStatResponse? ok, string? error)> SetSlotPointsAsync(
        string userId, string slotKey, int points, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slotKey))
            return (null, "SlotKey mangler.");

        string key = slotKey.Trim();
        var def = SlotDefs.FirstOrDefault(s => s.SlotKey.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(def.SlotKey))
            return (null, "Ukendt slotKey.");

        PlayerStat stat = await GetOrCreateStatAsync(userId, ct);
        await TrySeedBadgesAsync(stat, ct);

        // Outer slots may be locked until the inner badge is complete
        if (!CanSetOuterPoints(stat, key, def.Category, points, out string? lockError))
            return (null, lockError);

        BadgeProgress? badge = stat.BadgeProgresses
            .FirstOrDefault(b => b.BadgeKey.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (badge == null)
        {
            badge = new BadgeProgress { UserId = stat.UserId, BadgeKey = key };
            _db.BadgeProgresses.Add(badge);
            stat.BadgeProgresses.Add(badge);
        }

        badge.ApplyPoints(points);
        RecalcCategoryXp(stat);
        stat.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            string inner = ex.InnerException?.Message ?? ex.Message;
            _log.LogError(ex, "SetSlotPoints fejlede: {Inner}", inner);
            return (null, "Kunne ikke gemme slot: " + Trunc(inner, 180));
        }

        return (ToResponse(stat), null);
    }

    // +- category XP spread evenly across that category's slots (debug)
    public async Task<(PlayerStatResponse? ok, string? error)> AdjustAsync(
        string userId, string category, int delta, CancellationToken ct)
    {
        PlayerStat stat = await GetOrCreateStatAsync(userId, ct);
        await TrySeedBadgesAsync(stat, ct);

        string cat = category.Trim().ToLowerInvariant();
        var slots = SlotDefs.Where(s => s.Category == cat).Select(s => s.SlotKey).ToList();
        if (slots.Count == 0)
            return (null, "Ukendt kategori. Brug landbrugsproduktion|teknologi|klimahandling|energistyring|naturbevaring.");

        // Spread delta evenly as points on slots (~delta/5 points per button click)
        int pointDelta = Math.Clamp(delta / 5, -MaxPointsPerSlot, MaxPointsPerSlot);
        foreach (string slot in slots)
        {
            BadgeProgress? b = stat.BadgeProgresses
                .FirstOrDefault(x => x.BadgeKey.Equals(slot, StringComparison.OrdinalIgnoreCase));
            if (b == null) continue;
            int next = b.Points + pointDelta;
            // Skip outer slots that are still locked
            if (!CanSetOuterPoints(stat, slot, cat, next, out _))
                continue;
            b.ApplyPoints(next);
        }

        RecalcCategoryXp(stat);
        stat.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            string inner = ex.InnerException?.Message ?? ex.Message;
            return (null, "Kunne ikke gemme XP: " + Trunc(inner, 180));
        }

        return (ToResponse(stat), null);
    }

    // Enforces outer-slot locking when LockOuterUntilInnerComplete is enabled
    private bool CanSetOuterPoints(
        PlayerStat stat, string slotKey, string category, int points, out string? error)
    {
        error = null;
        // Zero/negative and unlocked modes always allowed
        if (points <= 0) return true;
        if (!_rules.LockOuterUntilInnerComplete) return true;
        if (InnerSlotKeys.Contains(slotKey)) return true;

        if (!InnerSlotByCategory.TryGetValue(category, out string? innerKey))
            return true;

        BadgeProgress? inner = stat.BadgeProgresses
            .FirstOrDefault(b => b.BadgeKey.Equals(innerKey, StringComparison.OrdinalIgnoreCase));
        int need = Math.Clamp(_rules.InnerCompletePoints, 1, MaxPointsPerSlot);
        if ((inner?.Points ?? 0) >= need) return true;

        error = $"Ydre låst — indre badge ({innerKey}) skal være optjent først.";
        return false;
    }

    // Recomputes per-category and total XP from badge point averages
    private static void RecalcCategoryXp(PlayerStat stat)
    {
        // XP columns keep legacy names; category keys match the badge wheel labels
        stat.AgriProductionXp = CategoryXp(stat, "landbrugsproduktion");
        stat.TechnologyXp = CategoryXp(stat, "teknologi");
        stat.ClimateActionXp = CategoryXp(stat, "klimahandling");
        stat.EnergiManagementXp = CategoryXp(stat, "energistyring");
        stat.NatureConservationXp = CategoryXp(stat, "naturbevaring");
        stat.TotalXp = stat.AgriProductionXp + stat.TechnologyXp + stat.ClimateActionXp + stat.EnergiManagementXp + stat.NatureConservationXp;
    }

    // Maps average badge points in a category to 0–MaxXpPerCategory XP
    private static int CategoryXp(PlayerStat stat, string category)
    {
        var keys = SlotDefs.Where(s => s.Category == category).Select(s => s.SlotKey).ToList();
        if (keys.Count == 0) return 0;
        double avg = 0;
        int n = 0;
        foreach (string key in keys)
        {
            BadgeProgress? b = stat.BadgeProgresses
                .FirstOrDefault(x => x.BadgeKey.Equals(key, StringComparison.OrdinalIgnoreCase));
            avg += b?.Points ?? 0;
            n++;
        }
        if (n == 0) return 0;
        avg /= n;
        // Map 0–300 avg → 0–1000 category XP
        return (int)Math.Round(avg / MaxPointsPerSlot * PlayerStat.MaxXpPerCategory);
    }

    // Loads the player's stats or creates a default row (handles create races)
    private async Task<PlayerStat> GetOrCreateStatAsync(string userId, CancellationToken ct)
    {
        PlayerStat? stat = await _db.PlayerStats
            .Include(p => p.BadgeProgresses)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (stat != null) return stat;

        bool userExists = await _db.Users.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
            throw new InvalidOperationException("Bruger findes ikke.");

        stat = PlayerStat.CreateDefaultForUser(userId);
        _db.PlayerStats.Add(stat);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Concurrent create: detach and reload the winner
            _log.LogWarning(ex, "PlayerStat create race/fejl for {UserId}: {Inner}",
                userId, ex.InnerException?.Message ?? ex.Message);
            _db.Entry(stat).State = EntityState.Detached;
            stat = await _db.PlayerStats
                .Include(p => p.BadgeProgresses)
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (stat == null)
                throw new InvalidOperationException(
                    "Kunne ikke oprette PlayerStat: " + (ex.InnerException?.Message ?? ex.Message));
        }

        return stat;
    }

    // Inserts any missing badge rows for the known slot keys (idempotent under races)
    private async Task TrySeedBadgesAsync(PlayerStat stat, CancellationToken ct)
    {
        var existing = new HashSet<string>(
            stat.BadgeProgresses.Select(b => b.BadgeKey),
            StringComparer.OrdinalIgnoreCase);

        var missing = BadgeKeys.Where(k => !existing.Contains(k)).ToList();
        if (missing.Count == 0) return;

        foreach (string key in missing)
        {
            _db.BadgeProgresses.Add(new BadgeProgress
            {
                UserId = stat.UserId,
                BadgeKey = key,
                Points = 0,
                Tier = "Gray",
                UpdatedAt = DateTime.UtcNow,
            });
        }

        try
        {
            await _db.SaveChangesAsync(ct);
            await _db.Entry(stat).Collection(p => p.BadgeProgresses).LoadAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Race or partial seed: drop pending inserts and reload from DB
            _log.LogWarning(ex, "Badge-seed fejlede for {UserId}: {Inner}",
                stat.UserId, ex.InnerException?.Message ?? ex.Message);
            foreach (var entry in _db.ChangeTracker.Entries<BadgeProgress>()
                         .Where(e => e.State == EntityState.Added)
                         .ToList())
                entry.State = EntityState.Detached;
            try { await _db.Entry(stat).Collection(p => p.BadgeProgresses).LoadAsync(ct); }
            catch { /* ignore reload failures after a seed race */ }
        }
    }

    // Converts category XP to a 0–100 percentage for the UI
    private static int Percent(int xp) =>
        (int)Math.Round(100.0 * Math.Clamp(xp, 0, PlayerStat.MaxXpPerCategory) / PlayerStat.MaxXpPerCategory);

    // Truncates long error text for API responses
    private static string Trunc(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    // Builds the API response with stable badge order (all known keys)
    private static PlayerStatResponse ToResponse(PlayerStat s)
    {
        var byKey = s.BadgeProgresses.ToDictionary(b => b.BadgeKey, StringComparer.OrdinalIgnoreCase);
        // Emit every known slot; missing rows show as empty Gray progress
        var badges = BadgeKeys
            .Select(k => byKey.TryGetValue(k, out BadgeProgress? b)
                ? new BadgeProgressDto(b.BadgeKey, b.Tier, b.Completions, b.BestCompletions, b.Points)
                : new BadgeProgressDto(k, "Gray", 0, 0, 0))
            .ToList();

        return new PlayerStatResponse(
            s.TotalXp,
            s.ClimateActionXp,
            s.NatureConservationXp,
            s.EnergiManagementXp,
            s.AgriProductionXp,
            s.TechnologyXp,
            Percent(s.ClimateActionXp),
            Percent(s.NatureConservationXp),
            Percent(s.EnergiManagementXp),
            Percent(s.AgriProductionXp),
            Percent(s.TechnologyXp),
            s.RoundsPlayed,
            badges);
    }
}
