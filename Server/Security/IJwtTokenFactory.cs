using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.Security;

/// <summary>
/// Creates JWT access tokens for authenticated users
/// </summary>
public interface IJwtTokenFactory
{
    // Builds a signed access token for the given user
    string CreateAccessToken(User user);
}
