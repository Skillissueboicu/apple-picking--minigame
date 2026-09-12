using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FarmerQuest.Server.Features.PlayerStats.DTOs;

namespace FarmerQuest.Server.Features.PlayerStats;

/// <summary>
/// Authenticated HTTP endpoints for the caller's player progress and debug adjustments
/// </summary>
[ApiController]
[Route("player-stats")]
[Authorize]
public sealed class PlayerStatController(PlayerStatService stats) : ControllerBase
{
    private readonly PlayerStatService _stats = stats;


    // Current user id from the JWT NameIdentifier claim
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Returns the caller's progress (creates/seeds stats if needed)
    [HttpGet("mine")]
    public async Task<ActionResult<PlayerStatResponse>> GetMine(CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        try
        {
            return Ok(await _stats.GetMineAsync(UserId, ct));
        }
        catch (Exception ex)
        {
            string detail = ex.InnerException?.Message ?? ex.Message;
            // Missing user usually means a stale JWT
            if (detail.Contains("Bruger findes ikke", StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new { error = "Session ugyldig. Log ind igen." });
            return StatusCode(500, new { error = "Kunne ikke hente progress.", detail });
        }
    }

    // Adjust XP in one category
    [HttpPost("adjust")]
    public async Task<ActionResult<PlayerStatResponse>> Adjust(
        [FromBody] AdjustXpRequest? request, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Category))
            return BadRequest(new { error = "Category mangler." });

        (PlayerStatResponse? ok, string? error) =
            await _stats.AdjustAsync(UserId, request.Category, request.Delta, ct);
        return ok != null ? Ok(ok) : BadRequest(new { error });
    }

    // DEBUG: set 0–300 points on one badge/slice (slotKey) 
    [HttpPost("slot")]
    public async Task<ActionResult<PlayerStatResponse>> SetSlot(
        [FromBody] SetSlotPointsRequest? request, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.SlotKey))
            return BadRequest(new { error = "SlotKey mangler." });

        (PlayerStatResponse? ok, string? error) =
            await _stats.SetSlotPointsAsync(UserId, request.SlotKey, request.Points, ct);
        return ok != null ? Ok(ok) : BadRequest(new { error });
    }
}
