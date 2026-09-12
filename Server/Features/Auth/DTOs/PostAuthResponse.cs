namespace FarmerQuest.Server.Features.Auth.DTOs;

// Response after successful register or login: JWT plus basic user info
public sealed class PostAuthResponse
{
    // Bearer access token
    public string Token { get; set; } = string.Empty;

    // Authenticated user profile
    public GetUserSimpleResponse User { get; set; } = new();
}

/*public sealed class GetUserInfoResponse
{
  public string Id { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Role { get; set; } = string.Empty;
}
*/
