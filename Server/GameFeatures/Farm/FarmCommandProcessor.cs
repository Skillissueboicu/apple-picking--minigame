namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Client command payload for farm session actions
/// </summary>
public sealed class FarmCommandRequest
{
    public string Type { get; set; } = "";
    public int SourceBuilding { get; set; }
    public int SourceResource { get; set; }
    public int TargetBuilding { get; set; }
    public string? ConnectionId { get; set; }
    public int Resource { get; set; }
    public int FocusedBuilding { get; set; } = -1;

    // Amount for borrow/repay 
    public int Amount { get; set; }
}

// Result of applying a farm command
public sealed class FarmCommandResult
{
    public bool Ok { get; init; }
    public string? Error { get; init; }
    public string? AppliedAction { get; init; }
    public int Version { get; init; }
    public string? StateJson { get; init; }
}

// Applies validated farm commands to session/farm state
public static class FarmCommandProcessor
{
    // Dispatches a command by type and returns success, error, and action tag
    public static (bool ok, string? error, string? action) Apply(
        FarmGameState farm,
        FarmSnapshotSerializer.FarmSessionSnapshot snapshot,
        FarmCommandRequest cmd,
        string userId)
    {
        return cmd.Type switch
        {
            "createConnection" => CreateConnection(farm, snapshot, cmd, userId),
            "removeConnection" => RemoveConnection(farm, cmd, userId),
            "retargetConnection" => RetargetConnection(farm, cmd, userId),
            "buy" => Buy(farm, cmd, userId),
            "sell" => Sell(farm, cmd, userId),
            "borrow" => Borrow(farm, cmd, userId),
            "repay" => Repay(farm, cmd, userId),
            "chargeInterest" => ChargeInterest(farm, cmd, userId),
            "inkassoOnce" => InkassoOnce(farm, cmd, userId),
            "addMoney" => AddMoney(farm, cmd, userId),
            "addCo2" => AddCo2(farm, cmd, userId),
            "setFocus" => SetFocus(snapshot, cmd, userId),
            _ => (false, "Ukendt kommando.", null),
        };
    }

    // Creates a new resource connection if it does not already exist
    private static (bool ok, string? error, string? action) CreateConnection(
        FarmGameState farm, FarmSnapshotSerializer.FarmSessionSnapshot snapshot, FarmCommandRequest cmd, string userId)
    {
        var source = (FarmBuildingId)cmd.SourceBuilding;
        var resource = (FarmResourceType)cmd.SourceResource;
        var target = (FarmBuildingId)cmd.TargetBuilding;

        if (source == target)
            return (false, "Kan ikke forbinde en bygning til sig selv.", null);

        // Reject duplicate source/resource/target triples
        if (farm.Connections.Any(c =>
                c.SourceBuilding == source &&
                c.SourceResource == resource &&
                c.TargetBuilding == target))
            return (false, "Den forbindelse findes allerede.", null);

        string id = Guid.NewGuid().ToString("N");
        farm.Connections.Add(new FarmConnection
        {
            Id = id,
            SourceBuilding = source,
            SourceResource = resource,
            TargetBuilding = target,
            CreatedByUserId = userId,
        });

        string action =
            $"createConnection:{id}:{FarmBuildingNames.GetLabel(source)}:" +
            $"{FarmResourceNames.GetLabel(resource)}:{FarmBuildingNames.GetLabel(target)}:{userId}";
        snapshot.LastAction = action;
        farm.EventLog.Add(
            $"Forbundet: {FarmBuildingNames.GetLabel(source)} ({FarmResourceNames.GetLabel(resource)}) " +
            $"→ {FarmBuildingNames.GetLabel(target)}");
        TrimLog(farm);
        return (true, null, action);
    }

    // Removes a connection by id
    private static (bool ok, string? error, string? action) RemoveConnection(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        if (string.IsNullOrWhiteSpace(cmd.ConnectionId))
            return (false, "Forbindelse mangler.", null);

        FarmConnection? conn = farm.Connections.FirstOrDefault(c => c.Id == cmd.ConnectionId);
        if (conn == null)
            return (false, "Forbindelsen findes ikke.", null);

        farm.Connections.Remove(conn);
        farm.EventLog.Add(
            $"Fjernet: {FarmBuildingNames.GetLabel(conn.SourceBuilding)} " +
            $"({FarmResourceNames.GetLabel(conn.SourceResource)}) → " +
            $"{FarmBuildingNames.GetLabel(conn.TargetBuilding)}");
        TrimLog(farm);
        return (true, null, $"removeConnection:{conn.Id}:{userId}");
    }

    // Changes the target building of an existing connection
    private static (bool ok, string? error, string? action) RetargetConnection(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        if (string.IsNullOrWhiteSpace(cmd.ConnectionId))
            return (false, "Forbindelse mangler.", null);

        FarmConnection? conn = farm.Connections.FirstOrDefault(c => c.Id == cmd.ConnectionId);
        if (conn == null)
            return (false, "Forbindelsen findes ikke.", null);

        var target = (FarmBuildingId)cmd.TargetBuilding;
        if (target == conn.SourceBuilding)
            return (false, "Mål kan ikke være samme bygning som kilden.", null);

        conn.TargetBuilding = target;
        farm.EventLog.Add(
            $"Flyttet: {FarmBuildingNames.GetLabel(conn.SourceBuilding)} " +
            $"({FarmResourceNames.GetLabel(conn.SourceResource)}) → " +
            $"{FarmBuildingNames.GetLabel(target)}");
        TrimLog(farm);
        return (true, null, $"retargetConnection:{conn.Id}:{(int)target}:{userId}");
    }

    // Buys seed or feed at fixed prices into the appropriate building
    private static (bool ok, string? error, string? action) Buy(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        var resource = (FarmResourceType)cmd.Resource;
        bool ok = resource switch
        {
            FarmResourceType.Fro => FarmSimulation.TryBuy(farm, resource, 50, 3, FarmBuildingId.Mark),
            FarmResourceType.Foder => FarmSimulation.TryBuy(farm, resource, 30, 5, FarmBuildingId.Stald),
            _ => false,
        };

        if (!ok)
            return (false, "Ikke nok penge.", null);

        return (true, null, $"buy:{(int)resource}:{userId}");
    }

    // Sells one unit of a resource from the barn store
    private static (bool ok, string? error, string? action) Sell(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        var resource = (FarmResourceType)cmd.Resource;
        int price = resource switch
        {
            FarmResourceType.Korn => 40,
            FarmResourceType.Maelk => 25,
            _ => 10,
        };

        int earned = FarmSimulation.TrySellFromLaden(farm, resource, price);
        if (earned == 0)
            return (false, $"Ingen {FarmResourceNames.GetLabel(resource).ToLower()} i laden.", null);

        return (true, null, $"sell:{(int)resource}:{userId}");
    }

    // Borrows cash via the bank service
    private static (bool ok, string? error, string? action) Borrow(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        (bool ok, string? error) = FarmBankService.TryBorrow(farm, cmd.Amount);
        if (!ok) return (false, error, null);
        return (true, null, $"borrow:{cmd.Amount}:{userId}");
    }

    // Repays debt via the bank service
    private static (bool ok, string? error, string? action) Repay(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        (bool ok, string? error, int paid) = FarmBankService.TryRepay(farm, cmd.Amount);
        if (!ok) return (false, error, null);
        return (true, null, $"repay:{paid}:{userId}");
    }

    // Charges one-shot interest via the bank service
    private static (bool ok, string? error, string? action) ChargeInterest(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        (bool ok, string? error, int interest) = FarmBankService.ChargeInterestOnce(farm, cmd.Amount);
        if (!ok) return (false, error, null);
        return (true, null, $"chargeInterest:{interest}:{userId}");
    }

    // Runs a one-shot collections seize via the bank service
    private static (bool ok, string? error, string? action) InkassoOnce(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        (bool ok, string? error, int seized) = FarmBankService.InkassoOnce(farm, cmd.Amount);
        if (!ok) return (false, error, null);
        return (true, null, $"inkassoOnce:{seized}:{userId}");
    }

    // Debug: adjusts money by the command amount
    private static (bool ok, string? error, string? action) AddMoney(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        (bool ok, string? error) = FarmBankService.AdjustMoney(farm, cmd.Amount);
        if (!ok) return (false, error, null);
        return (true, null, $"addMoney:{cmd.Amount}:{userId}");
    }

    // Debug: adjusts CO₂ by the command amount
    private static (bool ok, string? error, string? action) AddCo2(
        FarmGameState farm, FarmCommandRequest cmd, string userId)
    {
        (bool ok, string? error) = FarmBankService.AdjustCo2(farm, cmd.Amount);
        if (!ok) return (false, error, null);
        return (true, null, $"addCo2:{cmd.Amount}:{userId}");
    }

    // Updates which building the player is currently focused on (presence only)
    private static (bool ok, string? error, string? action) SetFocus(
        FarmSnapshotSerializer.FarmSessionSnapshot snapshot, FarmCommandRequest cmd, string userId)
    {
        if (!snapshot.Players.TryGetValue(userId, out FarmSnapshotSerializer.FarmPlayerPresenceState? presence))
            presence = new FarmSnapshotSerializer.FarmPlayerPresenceState { UserId = userId };

        presence.FocusedBuilding = cmd.FocusedBuilding;
        presence.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        snapshot.Players[userId] = presence;
        return (true, null, $"setFocus:{cmd.FocusedBuilding}:{userId}");
    }

    // Keeps the in-game event log to a short rolling window
    private static void TrimLog(FarmGameState farm)
    {
        while (farm.EventLog.Count > 8)
            farm.EventLog.RemoveAt(0);
    }
}
