namespace FarmerQuest.Server.Models;

/// <summary>
/// A participant in a GameSession
/// </summary>
public class GameSessionPlayer
{
    public long GameSessionPlayerId { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    // Display name shown in the lobby
    public string Name { get; set; } = string.Empty;

    // True when this player is the current temporary session host
    public bool IsHost { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public GameSession? Session { get; set; }
}
