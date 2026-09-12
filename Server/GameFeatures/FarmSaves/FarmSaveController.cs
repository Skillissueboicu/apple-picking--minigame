using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FarmerQuest.Server.GameFeatures.FarmSaves.DTOs;

namespace FarmerQuest.Server.GameFeatures.FarmSaves;

/// <summary>
/// REST API for listing, creating, renaming, and deleting a user's farm saves.
/// </summary>
[ApiController]
[Route("esg/farm-saves")]
[Authorize]
public sealed class FarmSaveController : ControllerBase
{
    private readonly FarmSaveService _saves;

    public FarmSaveController(FarmSaveService saves) => _saves = saves;

    // Authenticated user id from claims, or null if missing
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Display name from claims (fallback "Spiller")
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "Spiller";

    // Lists all farm saves owned by the current user
    [HttpGet]
    public async Task<ActionResult<List<FarmSaveResponse>>> List(CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        return Ok(await _saves.ListMineAsync(UserId, ct));
    }

    // Returns the most recently played farm save, or 404 if none
    [HttpGet("latest")]
    public async Task<ActionResult<FarmSaveResponse>> Latest(CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        FarmSaveResponse? save = await _saves.GetLatestAsync(UserId, ct);
        return save != null ? Ok(save) : NotFound();
    }

    // Creates a new farm save with optional display name
    [HttpPost]
    public async Task<ActionResult<FarmSaveResponse>> Create(
        [FromBody] CreateFarmSaveRequest? request, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (FarmSaveResponse? save, string? error) = await _saves.CreateAsync(UserId, UserName, request, ct);
        return save != null ? Ok(save) : BadRequest(new { error });
    }

    // Renames an owned farm save
    [HttpPatch("{saveId}")]
    public async Task<ActionResult<FarmSaveResponse>> Rename(
        string saveId, [FromBody] RenameFarmSaveRequest? request, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (FarmSaveResponse? save, string? error) = await _saves.RenameAsync(saveId, UserId, request, ct);
        return save != null ? Ok(save) : BadRequest(new { error });
    }

    // Deletes an owned farm save when it is not in an active session
    [HttpDelete("{saveId}")]
    public async Task<IActionResult> Delete(string saveId, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (bool ok, string? error) = await _saves.DeleteAsync(saveId, UserId, ct);
        return ok ? NoContent() : BadRequest(new { error });
    }
}
