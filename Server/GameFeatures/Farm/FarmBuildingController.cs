using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmerQuest.Server.GameFeatures.Farm;

[ApiController]
[Route("api/farm/buildings")]
[Authorize]
public class FarmBuildingController : ControllerBase
{
    private readonly FarmBuildingService _service;

    public FarmBuildingController(FarmBuildingService service)
    {
        _service = service;
    }

   private Guid GetUserId()
   {
    var userIdString =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!Guid.TryParse(userIdString, out var userId))
        throw new UnauthorizedAccessException("User ID mangler i token.");

    return userId;
    }

    [HttpGet]
    public async Task<ActionResult<List<BuildingPlacementDto>>> GetBuildings()
    {
    Guid userId = GetUserId();

    var buildings = await _service.GetBuildingsAsync(userId);

    return Ok(buildings);
    }
    [HttpPost]
    public async Task<ActionResult<BuildingPlacementDto>> PlaceBuilding(
    PlaceBuildingRequest request)

    {
    try
    {
        Guid userId = GetUserId();

        var building =
            await _service.PlaceBuildingAsync(userId, request);

        return Ok(building);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { message = ex.Message });
    }

    }

    [HttpDelete("{x:int}/{y:int}")]
    public async Task<IActionResult> RemoveBuilding(int x, int y)
    {
    Guid userId = GetUserId();

    bool removed =
        await _service.RemoveBuildingAsync(userId, x, y);

    if (!removed)
        return NotFound(new { message = "Ingen bygning fundet på positionen." });

    return NoContent();
    }

}