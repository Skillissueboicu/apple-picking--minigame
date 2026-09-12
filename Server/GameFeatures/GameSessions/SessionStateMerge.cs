using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmerQuest.Server.GameFeatures.GameSessions;

/// <summary>
/// Serverside merge of player updates into one session snapshot (shared farm + all players' presence). Supports 5+ players
/// </summary>
public static class SessionStateMerge
{
    private const int MaxPlayers = 16;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // True if another player can join (under the hard cap)
    public static bool CanJoin(int currentPlayerCount) => currentPlayerCount < MaxPlayers;

    /// <summary>
    /// Merges an incoming client update into the existing session JSON.
    /// Updates presence always; overwrites farm only when the update includes farm data.
    /// </summary>
    public static string Merge(string? existingJson, string incomingJson, string userId, string userName)
    {
        FarmSessionSnapshot snapshot = ParseSnapshot(existingJson);
        FarmClientUpdate? update = TryParseUpdate(incomingJson);

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string displayName = update?.SenderName ?? userName;
        int focused = update?.Presence?.FocusedBuilding ?? -1;

        // Always refresh this player's presence
        snapshot.Players[userId] = new FarmPlayerPresenceState
        {
            UserId = userId,
            Name = displayName,
            FocusedBuilding = focused,
            UpdatedAt = now,
        };

        // Farm payload present --> replace shared farm and last editor
        if (update?.Farm != null)
        {
            snapshot.Farm = update.Farm;
            snapshot.FarmVersion++;
            snapshot.LastFarmEditorId = userId;
        }

        // Drop players with no heartbeat for 2 minutes
        PruneStalePlayers(snapshot, now);

        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    // Removes players whose UpdatedAt is older than the stale threshold
    private static void PruneStalePlayers(FarmSessionSnapshot snapshot, long nowMs)
    {
        const long staleMs = 120_000;
        List<string> remove = snapshot.Players
            .Where(kv => nowMs - kv.Value.UpdatedAt > staleMs)
            .Select(kv => kv.Key)
            .ToList();
        foreach (string id in remove)
            snapshot.Players.Remove(id);
    }

    // Deserializes an existing snapshot, or returns empty on missing/legacy JSON
    private static FarmSessionSnapshot ParseSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new FarmSessionSnapshot();

        try
        {
            FarmSessionSnapshot? s = JsonSerializer.Deserialize<FarmSessionSnapshot>(json, JsonOptions);
            return s ?? new FarmSessionSnapshot();
        }
        catch
        {
            // Client format - start fresh
            return new FarmSessionSnapshot();
        }
    }

    // Parse of a client update; returns null if invalid
    private static FarmClientUpdate? TryParseUpdate(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<FarmClientUpdate>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    // Authoritative session snapshot: shared farm + per-player presence
    public sealed class FarmSessionSnapshot
    {
        // Monotonic farm edit counter for clients to detect newer state
        public int FarmVersion { get; set; }

        // UserId of the last player who submitted farm data
        public string? LastFarmEditorId { get; set; }

        // Opaque farm JSON element from the client
        public JsonElement? Farm { get; set; }

        // Presence keyed by UserId
        public Dictionary<string, FarmPlayerPresenceState> Players { get; set; } = new();
    }

    // Where a player is looking / last heartbeat
    public sealed class FarmPlayerPresenceState
    {
        public string UserId { get; set; } = "";
        public string Name { get; set; } = "";
        // Building index in focus, or -1 if none
        public int FocusedBuilding { get; set; } = -1;
        // Last presence update
        public long UpdatedAt { get; set; }
    }

    // Wire format for a client push into Merge
    public sealed class FarmClientUpdate
    {
        public string? SenderId { get; set; }
        public string? SenderName { get; set; }
        public JsonElement? Farm { get; set; }
        public FarmPlayerPresenceState? Presence { get; set; }
        public long SentAt { get; set; }
    }
}
