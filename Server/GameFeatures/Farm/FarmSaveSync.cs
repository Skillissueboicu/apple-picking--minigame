using FarmerQuest.Server.GameFeatures.Farm;
using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Copies session state into a FarmSave (summary fields + JSON).
/// Called on player actions and leave
/// </summary>
public static class FarmSaveSync
{
    // Updates the durable save from raw state JSON and extracts money/CO2/debt summary
    public static void ApplyFromState(FarmSave save, string? stateJson, int stateVersion)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
            return;

        // Persist raw snapshot + version timestamps
        save.StateJson = stateJson;
        save.StateVersion = stateVersion;
        save.UpdatedAt = DateTime.UtcNow;
        save.LastPlayedAt = DateTime.UtcNow;

        // Denormalize key economy fields for list/UI without parsing JSON each time
        FarmSnapshotSerializer.FarmSessionSnapshot snapshot = FarmSnapshotSerializer.Parse(stateJson);
        FarmGameState farm = FarmSnapshotSerializer.LoadFarm(snapshot);
        save.Money = farm.Money;
        save.Co2 = farm.Co2;
        save.Debt = farm.Debt;
    }

    // Convenience wrapper that reads state from a live GameSession
    public static void ApplyFromSession(FarmSave save, GameSession session) =>
        ApplyFromState(save, session.StateJson, session.StateVersion);
}
