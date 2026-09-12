namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Bank/debt - DEBUG/test values. Automatic interest/collections over time is disabled
/// </summary>
public static class FarmBankDefaults
{
    // Max debt allowed
    public const int MaxDebt = 1500;

    // Min. amount that can be borrowed per request
    public const int MinBorrow = 100;

    // Max. amount that can be borrowed per request
    public const int MaxBorrowPerRequest = 500;

    // Interest rate
    public const float InterestRateManual = 0.03f;

    // Default seize amount for inkasso
    public const int DefaultInkassoSeize = 100;
}
