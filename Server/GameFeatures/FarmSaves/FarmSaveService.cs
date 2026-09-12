using Microsoft.EntityFrameworkCore;

using FarmerQuest.Server.Databases;
using FarmerQuest.Server.GameFeatures.Farm;
using FarmerQuest.Server.GameFeatures.FarmSaves.DTOs;
using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.GameFeatures.FarmSaves;

/// <summary>
/// CRUD and ownership checks for durable farm saves
/// </summary>
public sealed class FarmSaveService
{
    // Maximum number of farm saves a single user may keep.</summary
    public const int MaxSavesPerUser = 20;

    private readonly FarmerQuestDbContext _db;

    public FarmSaveService(FarmerQuestDbContext db) => _db = db;

    // Lists the user's saves ordered by most recently played
    public async Task<List<FarmSaveResponse>> ListMineAsync(string userId, CancellationToken ct)
    {
        List<FarmSave> saves = await _db.FarmSaves
            .AsNoTracking()
            .Where(s => s.OwnerUserId == userId)
            .OrderByDescending(s => s.LastPlayedAt)
            .ToListAsync(ct);
        return saves.Select(ToResponse).ToList();
    }

    // Returns the user's most recently played save, or null if none
    public async Task<FarmSaveResponse?> GetLatestAsync(string userId, CancellationToken ct)
    {
        FarmSave? save = await _db.FarmSaves
            .AsNoTracking()
            .Where(s => s.OwnerUserId == userId)
            .OrderByDescending(s => s.LastPlayedAt)
            .FirstOrDefaultAsync(ct);
        return save == null ? null : ToResponse(save);
    }

    // Creates a new save with an initial farm snapshot, enforcing the per-user limit
    public async Task<(FarmSaveResponse? save, string? error)> CreateAsync(
        string userId, string userName, CreateFarmSaveRequest? request, CancellationToken ct)
    {
        int count = await _db.FarmSaves.CountAsync(s => s.OwnerUserId == userId, ct);
        if (count >= MaxSavesPerUser)
            return (null, $"Du kan højst have {MaxSavesPerUser} gemte gårde.");

        // Default name "Gård N" when the client omits DisplayName
        string name = string.IsNullOrWhiteSpace(request?.DisplayName)
            ? $"Gård {count + 1}"
            : request!.DisplayName!.Trim();
        if (name.Length > 80)
            name = name[..80];

        // Seed a fresh session snapshot + denormalized economy summary
        FarmSnapshotSerializer.FarmSessionSnapshot snapshot =
            FarmSnapshotSerializer.CreateInitial(userId, userName);
        string json = FarmSnapshotSerializer.Serialize(snapshot);
        FarmGameState farm = FarmSnapshotSerializer.LoadFarm(snapshot);

        var save = new FarmSave
        {
            OwnerUserId = userId,
            DisplayName = name,
            StateJson = json,
            StateVersion = 1,
            Money = farm.Money,
            Co2 = farm.Co2,
            Debt = farm.Debt,
        };

        _db.FarmSaves.Add(save);
        await _db.SaveChangesAsync(ct);
        return (ToResponse(save), null);
    }

    // Renames an owned save (max 80 characters)
    public async Task<(FarmSaveResponse? save, string? error)> RenameAsync(
        string saveId, string userId, RenameFarmSaveRequest? request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.DisplayName))
            return (null, "Navn mangler.");

        FarmSave? save = await _db.FarmSaves.FirstOrDefaultAsync(s => s.SaveId == saveId, ct);
        if (save == null) return (null, "Save ikke fundet.");
        if (save.OwnerUserId != userId) return (null, "Du ejer ikke dette save.");

        string name = request.DisplayName.Trim();
        if (name.Length > 80) name = name[..80];
        save.DisplayName = name;
        save.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (ToResponse(save), null);
    }

    // Deletes an owned save unless it is attached to a non-ended game session
    public async Task<(bool ok, string? error)> DeleteAsync(string saveId, string userId, CancellationToken ct)
    {
        FarmSave? save = await _db.FarmSaves.FirstOrDefaultAsync(s => s.SaveId == saveId, ct);
        if (save == null) return (false, "Save ikke fundet.");
        if (save.OwnerUserId != userId) return (false, "Du ejer ikke dette save.");

        // Block delete while the save is linked to an active session
        bool inUse = await _db.GameSessions.AnyAsync(s =>
            s.FarmSaveId == saveId && s.Status != "ended", ct);
        if (inUse)
            return (false, "Save er i brug i en aktiv session. Forlad spillet først.");

        _db.FarmSaves.Remove(save);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // Loads a save only if it belongs to the given user
    public async Task<FarmSave?> GetOwnedAsync(string saveId, string userId, CancellationToken ct) =>
        await _db.FarmSaves.FirstOrDefaultAsync(s => s.SaveId == saveId && s.OwnerUserId == userId, ct);

    // Maps an entity to the API response DTO
    public static FarmSaveResponse ToResponse(FarmSave s) =>
        new(
            s.SaveId,
            s.DisplayName,
            s.Money,
            s.Co2,
            s.Debt,
            s.StateVersion,
            new DateTimeOffset(s.CreatedAt).ToUnixTimeMilliseconds(),
            new DateTimeOffset(s.UpdatedAt).ToUnixTimeMilliseconds(),
            new DateTimeOffset(s.LastPlayedAt).ToUnixTimeMilliseconds());
}
