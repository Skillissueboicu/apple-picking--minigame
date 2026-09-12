namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Resolves what happens when a resource is delivered to a target building
/// </summary>
public static class FarmConsequences
{
    // Outcome after delivery (stored/wasted, climate deltas, message)
    public sealed class DeliveryResult
    {
        public bool Accepted = true;
        public int Stored;
        public int Wasted;
        public string Message = "";
        public float Co2Delta;
        public float ClimateDelta;
        public string? EffectId;
    }

    // Routes a delivery to the building specific handler
    public static DeliveryResult Apply(FarmGameState state, FarmBuildingId target,
        FarmResourceType resource, int amount)
    {
        var result = new DeliveryResult();
        FarmBuildingState building = state.GetBuilding(target);

        return target switch
        {
            FarmBuildingId.Stald => HandleStald(state, building, resource, amount, result),
            FarmBuildingId.Mark => HandleMark(state, building, resource, amount, result),
            FarmBuildingId.Kompost => HandleKompost(state, building, resource, amount, result),
            FarmBuildingId.Hovedhus => HandleHovedhus(state, building, resource, amount, result),
            FarmBuildingId.Laden => HandleLaden(state, building, resource, amount, result),
            _ => Waste(result, resource, amount, $"{FarmResourceNames.GetLabel(resource)} gik til spilde."),
        };
    }

    // Barn accepts feed; other resources are wasted with a CO2 penalty.
    private static DeliveryResult HandleStald(FarmGameState state, FarmBuildingState building,
        FarmResourceType resource, int amount, DeliveryResult result)
    {
        if (resource == FarmResourceType.Foder)
        {
            building.Buffer.Add(resource, amount);
            result.Stored = amount;
            result.Message = $"Foder til stalden (+{amount}).";
            return result;
        }

        // Wrong resource for barn --> waste + CO₂
        result.Wasted = amount;
        result.Message = $"{FarmResourceNames.GetLabel(resource)} passer ikke i stalden - gik til spilde.";
        state.Co2 += 0.5f * amount;
        return result;
    }

    // Field accepts seed/compost usefully; milk/slurry have waste or climate penalties
    private static DeliveryResult HandleMark(FarmGameState state, FarmBuildingState building,
        FarmResourceType resource, int amount, DeliveryResult result)
    {
        if (resource == FarmResourceType.Fro)
        {
            building.Buffer.Add(resource, amount);
            building.FieldPlanted = true;
            result.Stored = amount;
            result.Message = $"Frø på marken (+{amount}).";
            return result;
        }

        if (resource == FarmResourceType.Kompost)
        {
            building.Buffer.Add(resource, amount);
            result.Stored = amount;
            result.Message = $"Kompost spredt på marken (+{amount}).";
            result.ClimateDelta = 1f * amount;
            return result;
        }

        if (resource == FarmResourceType.Maelk)
        {
            result.Wasted = amount;
            result.Message = "Mælk hældes på marken og går til spilde.";
            state.Co2 += 0.3f * amount;
            LogEvent(state, result.Message);
            return result;
        }

        if (resource == FarmResourceType.Gylle)
        {
            // Slurry on the field is allowed but climate-hostile
            building.Buffer.Add(resource, amount);
            result.Stored = amount;
            result.Message = "Gylle på marken - underligt, men nu ligger det der.";
            result.Co2Delta = 2f * amount;
            result.ClimateDelta = -2f * amount;
            LogEvent(state, result.Message);
            return result;
        }

        result.Wasted = amount;
        result.Message = $"{FarmResourceNames.GetLabel(resource)} på marken - går til spilde.";
        LogEvent(state, result.Message);
        return result;
    }

    // Compost pile accepts any resource and resets the conversion timer
    private static DeliveryResult HandleKompost(FarmGameState state, FarmBuildingState building,
        FarmResourceType resource, int amount, DeliveryResult result)
    {
        building.Buffer.Add(resource, amount);
        result.Stored = amount;
        result.Message = $"{FarmResourceNames.GetLabel(resource)} lagt på kompostbunken (+{amount}).";
        // Fresh input restarts the conversion cycle
        building.CompostTimer = 0f;
        return result;
    }

    // Main house accepts milk/grain; slurry triggers the manure_house effect
    private static DeliveryResult HandleHovedhus(FarmGameState state, FarmBuildingState building,
        FarmResourceType resource, int amount, DeliveryResult result)
    {
        if (resource == FarmResourceType.Maelk)
        {
            building.Buffer.Add(resource, amount);
            result.Stored = amount;
            result.Message = $"Mælk til huset (+{amount}).";
            return result;
        }

        if (resource == FarmResourceType.Korn)
        {
            building.Buffer.Add(resource, amount);
            result.Stored = amount;
            result.Message = $"Korn til huset (+{amount}).";
            return result;
        }

        if (resource == FarmResourceType.Gylle)
        {
            building.Buffer.Add(resource, amount);
            result.Stored = amount;
            result.Message = "Gylle i hovedhuset - føj det stinker!";
            result.Co2Delta = 5f * amount;
            result.ClimateDelta = -3f * amount;
            // Persistent smell effect until cleared elsewhere
            if (!building.ActiveEffects.Contains("manure_house"))
                building.ActiveEffects.Add("manure_house");
            LogEvent(state, result.Message);
            return result;
        }

        building.Buffer.Add(resource, amount);
        result.Stored = amount;
        result.Message = $"{FarmResourceNames.GetLabel(resource)} i hovedhuset - underligt!";
        LogEvent(state, result.Message);
        return result;
    }

    // Barn store accepts sellable goods; odd items are stored with a note
    private static DeliveryResult HandleLaden(FarmGameState state, FarmBuildingState building,
        FarmResourceType resource, int amount, DeliveryResult result)
    {
        if (resource is FarmResourceType.Korn or FarmResourceType.Maelk)
        {
            building.Buffer.Add(resource, amount);
            result.Stored = amount;
            result.Message = $"{FarmResourceNames.GetLabel(resource)} klar til salg i laden (+{amount}).";
            return result;
        }

        building.Buffer.Add(resource, amount);
        result.Stored = amount;
        result.Message = $"{FarmResourceNames.GetLabel(resource)} i laden - måske kan det sælges?";
        LogEvent(state, result.Message);
        return result;
    }

    // Marks the delivery as fully wasted with a message
    private static DeliveryResult Waste(DeliveryResult result, FarmResourceType resource, int amount, string msg)
    {
        result.Wasted = amount;
        result.Message = msg;
        return result;
    }

    // Appends to the event log and trims to the last 8 entries.
    private static void LogEvent(FarmGameState state, string message)
    {
        state.EventLog.Add(message);
        if (state.EventLog.Count > 8)
            state.EventLog.RemoveAt(0);
    }
}
