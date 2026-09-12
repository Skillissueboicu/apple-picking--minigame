namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Serverside farm simulation: production cycles, connection delivery, and climate.
/// </summary>
public static class FarmSimulation
{
    // Seconds between hosted simulation ticks
    public const float TickInterval = 2f;

    // Barn production cycle length in seconds
    public const float StaldCycleSeconds = 8f;

    // Field production cycle length in seconds
    public const float MarkCycleSeconds = 12f;

    // Compost conversion cycle length in seconds
    public const float KompostCycleSeconds = 6f;

    // Hard cap per resource type in a building buffer
    public const int MaxBufferPerResource = 99;


    // Advances the farm by one simulation step
    public static void Tick(FarmGameState state, float deltaSeconds)
    {
        TickProduction(state, deltaSeconds);
        TickConnections(state);
        TickClimate(state);
        // Bank interest/collections are NOT run automatically - only via debug/gameplay commands
    }

    // Runs building production timers (barn, field, compost)
    private static void TickProduction(FarmGameState state, float deltaSeconds)
    {
        TickStald(state, deltaSeconds);
        TickMark(state, deltaSeconds);
        TickKompost(state, deltaSeconds);
    }

    // Barn consumes feed --> produce slurry + milk when the cycle completes
    private static void TickStald(FarmGameState state, float deltaSeconds)
    {
        FarmBuildingState stald = state.GetBuilding(FarmBuildingId.Stald);
        if (stald.AnimalCount <= 0) return;
        if (stald.Buffer.Get(FarmResourceType.Foder) <= 0) return;

        stald.ProductionTimer += deltaSeconds;
        if (stald.ProductionTimer < StaldCycleSeconds) return;
        stald.ProductionTimer = 0f;

        // One feed --> slurry + milk, plus a small CO2 cost
        stald.Buffer.TryTake(FarmResourceType.Foder, 1);
        TryAddToBuffer(stald, FarmResourceType.Gylle, 1);
        TryAddToBuffer(stald, FarmResourceType.Maelk, 1);
        state.Co2 += 0.2f;
    }

    // Field consumes seed (and optional compost bonus) --> produce grain
    private static void TickMark(FarmGameState state, float deltaSeconds)
    {
        FarmBuildingState mark = state.GetBuilding(FarmBuildingId.Mark);
        if (!mark.FieldPlanted) return;
        if (mark.Buffer.Get(FarmResourceType.Fro) <= 0) return;

        mark.ProductionTimer += deltaSeconds;
        if (mark.ProductionTimer < MarkCycleSeconds) return;
        mark.ProductionTimer = 0f;

        mark.Buffer.TryTake(FarmResourceType.Fro, 1);

        // Compost on the field grants +1 grain this cycle
        int bonus = mark.Buffer.Get(FarmResourceType.Kompost) > 0 ? 1 : 0;
        if (bonus > 0) mark.Buffer.TryTake(FarmResourceType.Kompost, 1);

        TryAddToBuffer(mark, FarmResourceType.Korn, 1 + bonus);
        state.Co2 += 0.1f;
    }

    // Compost pile converts one non-compost resource into compost and reduce CO2 slightly
    private static void TickKompost(FarmGameState state, float deltaSeconds)
    {
        FarmBuildingState kompost = state.GetBuilding(FarmBuildingId.Kompost);
        if (kompost.Buffer.IsEmpty) return;

        kompost.CompostTimer += deltaSeconds;
        if (kompost.CompostTimer < KompostCycleSeconds) return;
        kompost.CompostTimer = 0f;

        // Convert the first eligible buffer entry this cycle
        foreach (KeyValuePair<FarmResourceType, int> entry in kompost.Buffer.All.ToList())
        {
            if (entry.Key == FarmResourceType.Kompost) continue;
            if (!kompost.Buffer.TryTake(entry.Key, 1)) continue;
            TryAddToBuffer(kompost, FarmResourceType.Kompost, 1);
            state.Co2 -= 0.05f;
            break;
        }
    }

    // Moves one unit along each connection and applies target-building consequences
    private static void TickConnections(FarmGameState state)
    {
        foreach (FarmConnection conn in state.Connections)
        {
            FarmBuildingState source = state.GetBuilding(conn.SourceBuilding);
            if (!source.Buffer.TryTake(conn.SourceResource, 1)) continue;

            FarmConsequences.DeliveryResult result =
                FarmConsequences.Apply(state, conn.TargetBuilding, conn.SourceResource, 1);

            if (result.Co2Delta != 0f) state.Co2 += result.Co2Delta;
            if (result.ClimateDelta != 0f) state.ClimateScore += result.ClimateDelta;

            // Log only notable waste/effect outcomes (not every quiet delivery)
            if (!string.IsNullOrEmpty(result.Message) && (result.Wasted > 0 || result.EffectId != null))
                state.EventLog.Add(result.Message);
        }
    }

    // Applies ongoing climate effects and clamps score/CO2 ranges
    private static void TickClimate(FarmGameState state)
    {
        // Ongoing penalty while manure effect is active on a building
        foreach (FarmBuildingState building in state.Buildings.Values)
        {
            if (building.ActiveEffects.Contains("manure_house"))
                state.Co2 += 0.05f;
        }

        state.ClimateScore = Math.Clamp(state.ClimateScore, 0f, 100f);
        state.Co2 = Math.Max(0f, state.Co2);
    }

    // Adds to a buffer without exceeding MaxBufferPerResource
    private static void TryAddToBuffer(FarmBuildingState building, FarmResourceType type, int amount)
    {
        int current = building.Buffer.Get(type);
        int add = Math.Min(amount, MaxBufferPerResource - current);
        if (add > 0) building.Buffer.Add(type, add);
    }

    // Buys a resource if the player can afford it and delivers it to a building
    public static bool TryBuy(FarmGameState state, FarmResourceType resource, int price, int amount,
        FarmBuildingId deliverTo)
    {
        if (state.Money < price) return false;
        state.Money -= price;
        FarmBuildingState building = state.GetBuilding(deliverTo);

        // Buying seed for the field also marks it planted
        if (resource == FarmResourceType.Fro && deliverTo == FarmBuildingId.Mark)
            building.FieldPlanted = true;

        TryAddToBuffer(building, resource, amount);
        return true;
    }

    // Sells one unit of a resource from the barn store; returns earnings (0 if empty)
    public static int TrySellFromLaden(FarmGameState state, FarmResourceType resource, int unitPrice)
    {
        FarmBuildingState laden = state.GetBuilding(FarmBuildingId.Laden);
        int have = laden.Buffer.Get(resource);
        if (have <= 0) return 0;

        laden.Buffer.TryTake(resource, 1);
        state.Money += unitPrice;
        return unitPrice;
    }
}
