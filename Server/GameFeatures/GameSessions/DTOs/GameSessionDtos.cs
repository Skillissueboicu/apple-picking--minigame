namespace FarmerQuest.Server.GameFeatures.GameSessions.DTOs;

// Create a new session. Mode matches Unity GameMode (0/1/2). FarmSaveId loads an existing save
public sealed record CreateSessionRequest(int Mode, string? GameKind, string? FarmSaveId = null);

// Join a session by invite code
public sealed record JoinByCodeRequest(string Code);

// Invite a player by username
public sealed record InviteRequest(string Username);

// A player submits game state as JSON for the others
public sealed record SubmitStateRequest(string StateJson);

// Client requests a change - server validates and broadcasts a snapshot
public sealed record FarmCommandRequestDto(
    string Type,
    int SourceBuilding = 0,
    int SourceResource = 0,
    int TargetBuilding = 0,
    string? ConnectionId = null,
    int Resource = 0,
    int FocusedBuilding = -1,
    int Amount = 0);

// Result of a farm command: success/error plus optional new state
public sealed record FarmCommandResponse(
    bool Ok,
    string? Error,
    string? AppliedAction,
    int Version,
    string? StateJson);

// Latest game state for spectators (REST polling)
public sealed record SessionStateResponse(int Version, string? StateJson);

// Pending invite payload pushed to the invited user
public sealed record SessionInvitationResponse(
    long InvitationId,
    string SessionId,
    string Code,
    string HostUserId,
    string HostName,
    string? GameKind,
    string Status,
    long CreatedAt);

// One player listed in a session response
public sealed record SessionPlayerResponse(
    string UserId,
    string Name,
    bool IsHost,
    bool IsActiveDriver);

// Full session snapshot for lobby/game UI
public sealed record SessionResponse(
    string SessionId,
    string Code,
    int Mode,
    string HostUserId,
    string? ActiveDriverUserId,
    string Status,
    string? GameKind,
    string? FarmSaveId,
    List<SessionPlayerResponse> Players);
