namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Per-building inventory of resources
/// </summary>
public sealed class ResourceBuffer
{
    private readonly Dictionary<FarmResourceType, int> _amounts = new();

    // Returns the current amount of a resource (0 if missing)
    public int Get(FarmResourceType type) => _amounts.TryGetValue(type, out int v) ? v : 0;

    // Sets an amount; removes the entry when amount =< 0
    public void Set(FarmResourceType type, int amount)
    {
        if (amount <= 0) _amounts.Remove(type);
        else _amounts[type] = amount;
    }

    // Adds to the current amount (no-op for non-positive deltas)
    public void Add(FarmResourceType type, int amount)
    {
        if (amount <= 0) return;
        Set(type, Get(type) + amount);
    }

    // Removes amount if available; returns false when the buffer is short
    public bool TryTake(FarmResourceType type, int amount)
    {
        int have = Get(type);
        if (have < amount) return false;
        Set(type, have - amount);
        return true;
    }

    // Positive resource entries only
    public IEnumerable<KeyValuePair<FarmResourceType, int>> All =>
        _amounts.Where(kv => kv.Value > 0);

    // True when no positive amounts remain
    public bool IsEmpty => !_amounts.Any(kv => kv.Value > 0);
}

// A directed resource flow from one building's buffer to another building
public sealed class FarmConnection
{
    public string Id { get; set; } = "";
    public FarmBuildingId SourceBuilding { get; set; }
    public FarmResourceType SourceResource { get; set; }
    public FarmBuildingId TargetBuilding { get; set; }
    public string CreatedByUserId { get; set; } = "";
}

// Runtime state for a single farm building
public sealed class FarmBuildingState
{
    public FarmBuildingId Id { get; set; }
    public ResourceBuffer Buffer { get; set; } = new();

    // Animals in the barn (production requires at least one)
    public int AnimalCount { get; set; }

    // Whether the field has been planted (enables crop cycles)
    public bool FieldPlanted { get; set; }

    // Seconds accumulated toward the next production cycle 
    public float ProductionTimer { get; set; }

    // Seconds accumulated toward the next compost conversion 
    public float CompostTimer { get; set; }

    // Active gameplay effect ids (e.g. manure/slurry in the house) 
    public List<string> ActiveEffects { get; set; } = new();
}

// Full authoritative farm game state shared by all players in a session
public sealed class FarmGameState
{
    public int Money { get; set; } = FarmEconomyDefaults.StartingMoney;
    public float Co2 { get; set; } = FarmEconomyDefaults.StartingCo2;
    public float ClimateScore { get; set; } = FarmEconomyDefaults.StartingClimateScore;

    // Outstanding bank debt (including charged interest)
    public int Debt { get; set; }

    public int TotalBorrowed { get; set; }
    public int TotalRepaid { get; set; }
    public int TotalInterestCharged { get; set; }

    // Bank cycle timer
    public float BankTimer { get; set; }

    // Missed bank cycles
    public int MissedBankCycles { get; set; }

    // True while collections are considered active after a seize
    public bool InkassoActive { get; set; }

    // Legacy flag for interest scheduling
    public bool RepaidSinceLastInterest { get; set; } = true;

    public Dictionary<FarmBuildingId, FarmBuildingState> Buildings { get; set; } = new();
    public List<FarmConnection> Connections { get; set; } = new();
    public List<string> EventLog { get; set; } = new();

    // Returns existing building state or creates an empty one for the id
    public FarmBuildingState GetBuilding(FarmBuildingId id)
    {
        if (!Buildings.TryGetValue(id, out FarmBuildingState? b))
        {
            b = new FarmBuildingState { Id = id };
            Buildings[id] = b;
        }
        return b;
    }

    // Creates a new farm with starting economy and seed animals/feed in the barn
    public static FarmGameState CreateNew()
    {
        var state = new FarmGameState
        {
            Money = FarmEconomyDefaults.StartingMoney,
            Co2 = FarmEconomyDefaults.StartingCo2,
            ClimateScore = FarmEconomyDefaults.StartingClimateScore,
        };

        // Seed barn with animals + starter feed
        FarmBuildingState stald = state.GetBuilding(FarmBuildingId.Stald);
        stald.AnimalCount = 2;
        stald.Buffer.Add(FarmResourceType.Foder, 5);

        // Ensure all building slots exist in the dictionary
        state.GetBuilding(FarmBuildingId.Mark);
        state.GetBuilding(FarmBuildingId.Kompost);
        state.GetBuilding(FarmBuildingId.Hovedhus);
        state.GetBuilding(FarmBuildingId.Laden);
        return state;
    }
}
