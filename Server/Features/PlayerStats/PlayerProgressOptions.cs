using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.Features.PlayerStats;

/// <summary>
/// Progress rules. Configure under "PlayerProgress" in appsettings.json.
/// LockOuterUntilInnerComplete = false --> all outer slots can be earned from the start
/// </summary>
public sealed class PlayerProgressOptions
{
    public const string SectionName = "PlayerProgress";

    /// <summary>
    /// true: outer slots cannot gain points until the inner badge in the same category is earned.
    /// false: no lock 
    /// </summary>
    public bool LockOuterUntilInnerComplete { get; set; } = true;

    // Points required on the inner badge before outer slots unlock (default = max)
    public int InnerCompletePoints { get; set; } = BadgeProgress.MaxPoints;
}
