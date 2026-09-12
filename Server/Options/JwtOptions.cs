namespace FarmerQuest.Server.Options;

/// <summary>
/// JWT settings bound from the "Jwt" configuration section
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // Symmetric signing key (required)
    public string Key { get; set; } = string.Empty;

    // Token issuer claim
    public string Issuer { get; set; } = string.Empty;

    // Token audience claim
    public string Audience { get; set; } = string.Empty;

    // Access-token lifetime in days (default 30)
    public int ExpiryDays { get; set; } = 30;
}
