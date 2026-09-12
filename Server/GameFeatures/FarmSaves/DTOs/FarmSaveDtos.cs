namespace FarmerQuest.Server.GameFeatures.FarmSaves.DTOs;

/// <summary>
/// Request body for creating a new farm save
/// </summary>
/// <param name="DisplayName">Optional display name</param>
public sealed record CreateFarmSaveRequest(string? DisplayName);

/// <summary>
/// Request body for renaming an existing farm save
/// </summary>
/// <param name="DisplayName">New display name (required)</param>
public sealed record RenameFarmSaveRequest(string DisplayName);

/// <summary>
/// API response for a farm save
/// </summary>
public sealed record FarmSaveResponse(
    string SaveId,
    string DisplayName,
    int Money,
    float Co2,
    int Debt,
    int StateVersion,
    long CreatedAt,
    long UpdatedAt,
    long LastPlayedAt);
