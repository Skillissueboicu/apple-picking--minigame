namespace FarmerQuest.Server.Features.PlayerStats.DTOs;

// Progress for a single badge/slice slot
public sealed record BadgeProgressDto(
    string BadgeKey,
    string Tier,
    int Completions,
    int BestCompletions,
    int Points);

// Full player progress payload: category XP, percents, rounds, and all badges
public sealed record PlayerStatResponse(
    int TotalXp,
    int MarkenXp,
    int NaturenXp,
    int EnergiXp,
    int MadenXp,
    int HoldetXp,
    int MarkenPercent,
    int NaturenPercent,
    int EnergiPercent,
    int MadenPercent,
    int HoldetPercent,
    int RoundsPlayed,
    List<BadgeProgressDto> Badges);

// Legacy category XP adjustment (debug)
public sealed record AdjustXpRequest(string Category, int Delta);

// Set points (0–300) on one badge/slice slot 
public sealed record SetSlotPointsRequest(string SlotKey, int Points);
