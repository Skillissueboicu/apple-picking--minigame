using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FarmerQuest.Server.GameFeatures.GameSessions.DTOs;
using FarmerQuest.Server.GameFeatures.Farm;

namespace FarmerQuest.Server.GameFeatures.GameSessions;

/// <summary>
/// REST API for lobby/session lifecycle, invites, farm commands, and state sync
/// </summary>
[ApiController]
[Route("esg/game-sessions")]
[Authorize]
public sealed class GameSessionController : ControllerBase
{
    private readonly GameSessionService _sessions;
    private readonly FarmSessionAuthority _farmAuthority;

    public GameSessionController(GameSessionService sessions, FarmSessionAuthority farmAuthority)
    {
        _sessions = sessions;
        _farmAuthority = farmAuthority;
    }

    // Authenticated user's id from JWT NameIdentifier
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Display name from JWT, with a fallback for missing claims
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "Spiller";

    // Create a session (or resume an active farm save session)
    [HttpPost]
    public async Task<ActionResult<SessionResponse>> Create(
        [FromBody] CreateSessionRequest? request, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        if (request == null) return BadRequest(new { error = "Request body mangler." });

        (SessionResponse? dto, string? error) = await _sessions.CreateAsync(UserId, UserName, request, ct);
        return dto != null
            ? Created($"/esg/game-sessions/{dto.Code}", dto)
            : BadRequest(new { error });
    }

    // Join an existing session by short code
    [HttpPost("join")]
    public async Task<ActionResult<SessionResponse>> Join(
        [FromBody] JoinByCodeRequest? request, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        if (request == null) return BadRequest(new { error = "Request body mangler." });

        (SessionResponse? session, string? error) = await _sessions.JoinByCodeAsync(
            UserId, UserName, request.Code, ct);
        return session != null ? Ok(session) : BadRequest(new { error });
    }

    // List pending invitations for the current user
    [HttpGet("invitations/mine")]
    public async Task<ActionResult<List<SessionInvitationResponse>>> GetMyInvitations(CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        return Ok(await _sessions.GetPendingInvitationsAsync(UserId, ct));
    }

    // Accept an invitation and join its session
    [HttpPost("invitations/{invitationId:long}/accept")]
    public async Task<ActionResult<SessionResponse>> AcceptInvitation(long invitationId, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (SessionResponse? session, string? error) =
            await _sessions.AcceptInvitationAsync(invitationId, UserId, UserName, ct);
        return session != null ? Ok(session) : BadRequest(new { error });
    }

    // Decline a pending invitation
    [HttpPost("invitations/{invitationId:long}/decline")]
    public async Task<IActionResult> DeclineInvitation(long invitationId, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (bool ok, string? error) = await _sessions.DeclineInvitationAsync(invitationId, UserId, ct);
        return ok ? NoContent() : BadRequest(new { error });
    }

    // Look up a session by join code
    [HttpGet("{code}")]
    public async Task<ActionResult<SessionResponse>> GetByCode(string code, CancellationToken ct)
    {
        SessionResponse? dto = await _sessions.GetByCodeAsync(code, ct);
        return dto != null ? Ok(dto) : NotFound();
    }

    // Active (non-ended) sessions the current user is in 
    [HttpGet("mine")]
    public async Task<ActionResult<List<SessionResponse>>> GetMine(CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        return Ok(await _sessions.GetMineAsync(UserId, ct));
    }

    // Host starts the game (loads farm save into live state when applicable)
    [HttpPost("{sessionId}/start")]
    public async Task<ActionResult<SessionResponse>> Start(string sessionId, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (SessionResponse? session, string? error) = await _sessions.StartAsync(sessionId, UserId, ct);
        return session != null ? Ok(session) : BadRequest(new { error });
    }

    // Host invites another user by username
    [HttpPost("{sessionId}/invite")]
    public async Task<ActionResult<SessionResponse>> Invite(
        string sessionId, [FromBody] InviteRequest? request, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        if (request == null) return BadRequest(new { error = "Request body mangler." });

        (SessionResponse? session, string? error) = await _sessions.InviteByUsernameAsync(
            sessionId, UserId, request.Username, ct);
        return session != null ? Ok(session) : BadRequest(new { error });
    }

    // Leave a session; may transfer host or end the session
    [HttpPost("{sessionId}/leave")]
    public async Task<IActionResult> Leave(string sessionId, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (bool ok, string? error) = await _sessions.LeaveAsync(sessionId, UserId, ct);
        return ok ? NoContent() : BadRequest(new { error });
    }

    // Execute a validated farm command via the farm session authority
    [HttpPost("{sessionId}/farm/command")]
    public async Task<ActionResult<FarmCommandResponse>> FarmCommand(
        string sessionId, [FromBody] FarmCommandRequestDto? request, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        if (request == null) return BadRequest(new { error = "Request body mangler." });

        // Map DTO --> internal command shape used by FarmSessionAuthority
        FarmCommandResult result = await _farmAuthority.ExecuteCommandAsync(
            sessionId,
            UserId,
            UserName,
            new FarmCommandRequest
            {
                Type = request.Type,
                SourceBuilding = request.SourceBuilding,
                SourceResource = request.SourceResource,
                TargetBuilding = request.TargetBuilding,
                ConnectionId = request.ConnectionId,
                Resource = request.Resource,
                FocusedBuilding = request.FocusedBuilding,
                Amount = request.Amount,
            },
            ct);

        return Ok(new FarmCommandResponse(
            result.Ok,
            result.Error,
            result.AppliedAction,
            result.Version,
            result.StateJson));
    }

    // Client-pushed state for non-farm games (server broadcasts to the group)
    [HttpPost("{sessionId}/state")]
    public async Task<ActionResult<SessionStateResponse>> SubmitState(
        string sessionId, [FromBody] SubmitStateRequest? request, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        if (request == null) return BadRequest(new { error = "Request body mangler." });

        (SessionStateResponse? state, string? error) =
            await _sessions.SubmitStateAsync(sessionId, UserId, UserName, request.StateJson, ct);
        return state != null ? Ok(state) : BadRequest(new { error });
    }

    // Poll the latest session state version + JSON
    [HttpGet("{sessionId}/state")]
    public async Task<ActionResult<SessionStateResponse>> GetState(string sessionId, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        SessionStateResponse? state = await _sessions.GetStateAsync(sessionId, ct);
        return state != null ? Ok(state) : NotFound();
    }
}
