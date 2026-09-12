using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Serializes/deserializes farm session snapshots and nested farm state DTOs
/// </summary>
public static class FarmSnapshotSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // Parses session JSON; returns an empty snapshot on null/invalid input
    public static FarmSessionSnapshot Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return CreateEmpty();

        try
        {
            FarmSessionSnapshot? snapshot = JsonSerializer.Deserialize<FarmSessionSnapshot>(json, JsonOptions);
            if (snapshot == null) return CreateEmpty();
            // Ensure players map is never null after deserialize
            snapshot.Players ??= new Dictionary<string, FarmPlayerPresenceState>();
            return snapshot;
        }
        catch
        {
            // Corrupt JSON --> start fresh rather than crash the session
            return CreateEmpty();
        }
    }

    // Serializes a session snapshot to camelCase JSON
    public static string Serialize(FarmSessionSnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, JsonOptions);

    // Loads runtime farm state from the nested farm JSON element
    public static FarmGameState LoadFarm(FarmSessionSnapshot snapshot)
    {
        if (snapshot.Farm == null)
            return FarmGameState.CreateNew();

        try
        {
            FarmStateDto? dto = JsonSerializer.Deserialize<FarmStateDto>(snapshot.Farm.Value.GetRawText(), JsonOptions);
            return dto == null ? FarmGameState.CreateNew() : FromDto(dto);
        }
        catch
        {
            return FarmGameState.CreateNew();
        }
    }

    // Writes runtime farm state back into the snapshot's farm element
    public static void SaveFarm(FarmSessionSnapshot snapshot, FarmGameState farm)
    {
        snapshot.Farm = JsonSerializer.SerializeToElement(ToDto(farm), JsonOptions);
    }

    // Creates a new session snapshot with a fresh farm and the owner's presence
    public static FarmSessionSnapshot CreateInitial(string userId, string userName)
    {
        var snapshot = CreateEmpty();
        var farm = FarmGameState.CreateNew();
        SaveFarm(snapshot, farm);
        snapshot.FarmVersion = 1;
        snapshot.Players[userId] = NewPresence(userId, userName, -1);
        return snapshot;
    }

    // Empty snapshot with an initialized players dictionary
    public static FarmSessionSnapshot CreateEmpty() => new()
    {
        Players = new Dictionary<string, FarmPlayerPresenceState>(),
    };

    // Builds a presence entry with current UTC timestamp
    public static FarmPlayerPresenceState NewPresence(string userId, string userName, int focusedBuilding) =>
        new()
        {
            UserId = userId,
            Name = userName,
            FocusedBuilding = focusedBuilding,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

    // Maps a persisted DTO into runtime FarmGameState
    private static FarmGameState FromDto(FarmStateDto dto)
    {
        var state = new FarmGameState
        {
            Money = dto.Money,
            Co2 = dto.Co2,
            ClimateScore = dto.ClimateScore,
            Debt = dto.Debt,
            TotalBorrowed = dto.TotalBorrowed,
            TotalRepaid = dto.TotalRepaid,
            TotalInterestCharged = dto.TotalInterestCharged,
            BankTimer = dto.BankTimer,
            MissedBankCycles = dto.MissedBankCycles,
            InkassoActive = dto.InkassoActive,
            RepaidSinceLastInterest = dto.RepaidSinceLastInterest,
            EventLog = dto.EventLog ?? new List<string>(),
        };

        foreach (FarmConnectionDto conn in dto.Connections ?? new List<FarmConnectionDto>())
        {
            state.Connections.Add(new FarmConnection
            {
                Id = conn.Id ?? "",
                SourceBuilding = (FarmBuildingId)conn.SourceBuilding,
                SourceResource = (FarmResourceType)conn.SourceResource,
                TargetBuilding = (FarmBuildingId)conn.TargetBuilding,
                CreatedByUserId = conn.CreatedByUserId ?? "",
            });
        }

        // Overlay DTO building fields onto lazily created building slots
        foreach (FarmBuildingDto b in dto.Buildings ?? new List<FarmBuildingDto>())
        {
            FarmBuildingState building = state.GetBuilding((FarmBuildingId)b.Id);
            building.AnimalCount = b.AnimalCount;
            building.FieldPlanted = b.FieldPlanted;
            building.ProductionTimer = b.ProductionTimer;
            building.CompostTimer = b.CompostTimer;
            building.ActiveEffects = b.ActiveEffects ?? new List<string>();

            foreach (FarmBufferEntryDto entry in b.Buffer ?? new List<FarmBufferEntryDto>())
                building.Buffer.Set((FarmResourceType)entry.Resource, entry.Amount);
        }

        return state;
    }

    // Maps runtime farm state into a JSON-friendly DTO
    private static FarmStateDto ToDto(FarmGameState state)
    {
        var dto = new FarmStateDto
        {
            Money = state.Money,
            Co2 = state.Co2,
            ClimateScore = state.ClimateScore,
            Debt = state.Debt,
            TotalBorrowed = state.TotalBorrowed,
            TotalRepaid = state.TotalRepaid,
            TotalInterestCharged = state.TotalInterestCharged,
            BankTimer = state.BankTimer,
            MissedBankCycles = state.MissedBankCycles,
            InkassoActive = state.InkassoActive,
            RepaidSinceLastInterest = state.RepaidSinceLastInterest,
            EventLog = new List<string>(state.EventLog),
            Connections = state.Connections.Select(c => new FarmConnectionDto
            {
                Id = c.Id,
                SourceBuilding = (int)c.SourceBuilding,
                SourceResource = (int)c.SourceResource,
                TargetBuilding = (int)c.TargetBuilding,
                CreatedByUserId = c.CreatedByUserId,
            }).ToList(),
        };

        foreach (KeyValuePair<FarmBuildingId, FarmBuildingState> kv in state.Buildings)
        {
            var b = new FarmBuildingDto
            {
                Id = (int)kv.Key,
                AnimalCount = kv.Value.AnimalCount,
                FieldPlanted = kv.Value.FieldPlanted,
                ProductionTimer = kv.Value.ProductionTimer,
                CompostTimer = kv.Value.CompostTimer,
                ActiveEffects = new List<string>(kv.Value.ActiveEffects),
            };
            foreach (KeyValuePair<FarmResourceType, int> entry in kv.Value.Buffer.All)
            {
                b.Buffer.Add(new FarmBufferEntryDto
                {
                    Resource = (int)entry.Key,
                    Amount = entry.Value,
                });
            }
            dto.Buildings.Add(b);
        }

        return dto;
    }

    // Top level multiplayer session snapshot stored in GameSession.StateJson
    public sealed class FarmSessionSnapshot
    {
        public int FarmVersion { get; set; }

        // Last user who applied a farm action
        public string? LastFarmEditorId { get; set; }

        // Nested farm state as a JSON element
        public JsonElement? Farm { get; set; }

        // Per-player presence (focus, name, last update)
        public Dictionary<string, FarmPlayerPresenceState> Players { get; set; } = new();

        // Last applied action tag for UI/debug
        public string? LastAction { get; set; }
    }

    // Presence state for a player in session
    public sealed class FarmPlayerPresenceState
    {
        public string UserId { get; set; } = "";
        public string Name { get; set; } = "";

        // Focused building index, or -1 if none
        public int FocusedBuilding { get; set; } = -1;

        // Last update time as Unix milliseconds
        public long UpdatedAt { get; set; }
    }

    // Persistable farm economy/buildings/connections DTO
    public sealed class FarmStateDto
    {
        public int Money { get; set; }
        public float Co2 { get; set; }
        public float ClimateScore { get; set; }
        public int Debt { get; set; }
        public int TotalBorrowed { get; set; }
        public int TotalRepaid { get; set; }
        public int TotalInterestCharged { get; set; }
        public float BankTimer { get; set; }
        public int MissedBankCycles { get; set; }
        public bool InkassoActive { get; set; }
        public bool RepaidSinceLastInterest { get; set; } = true;
        public List<FarmBuildingDto> Buildings { get; set; } = new();
        public List<FarmConnectionDto> Connections { get; set; } = new();
        public List<string>? EventLog { get; set; }
    }

    // Persistable building state DTO
    public sealed class FarmBuildingDto
    {
        public int Id { get; set; }
        public List<FarmBufferEntryDto> Buffer { get; set; } = new();
        public int AnimalCount { get; set; }
        public bool FieldPlanted { get; set; }
        public float ProductionTimer { get; set; }
        public float CompostTimer { get; set; }
        public List<string>? ActiveEffects { get; set; }
    }

    // One resource amount in a building buffer
    public sealed class FarmBufferEntryDto
    {
        public int Resource { get; set; }
        public int Amount { get; set; }
    }

    // Persistable connection DTO (ids stored as ints for JSON)
    public sealed class FarmConnectionDto
    {
        public string? Id { get; set; }
        public int SourceBuilding { get; set; }
        public int SourceResource { get; set; }
        public int TargetBuilding { get; set; }
        public string? CreatedByUserId { get; set; }
    }
}
