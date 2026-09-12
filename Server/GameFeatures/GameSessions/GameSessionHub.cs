using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FarmerQuest.Server.GameFeatures.GameSessions;

/// <summary>
/// Realtime hub for lobby + multiplayer. Clients connect, join a session group (sessionId), and receive push events (SessionUpdated, GameStarted,
/// StateReceived). REST SubmitState merges state on the server and broadcasts to the whole group - all players play together (up to 16).
/// </summary>
[Authorize]
public sealed class GameSessionHub : Hub
{
    // Hub route mapped in Program.cs
    public const string Path = "/hubs/game-session";

    // Subscribe this connection to a session's broadcast group
    public Task JoinGroup(string sessionId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

    // Leave a session's broadcast group
    public Task LeaveGroup(string sessionId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

    // Direct relay. Preferred path is REST SubmitState with server merge
    public Task SendState(string sessionId, string stateJson) =>
        Clients.Group(sessionId).SendAsync("StateReceived", stateJson);
}
