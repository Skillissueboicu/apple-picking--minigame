using Microsoft.EntityFrameworkCore;
using FarmerQuest.Server.GameFeatures.Farm;
using FarmerQuest.Server.Databases;

namespace FarmerQuest.Server.GameFeatures.Farm;

public class FarmBuildingService
{
    private readonly FarmerQuestDbContext _db;

    public FarmBuildingService(FarmerQuestDbContext db)
    {
        _db = db;
    }

    public async Task<List<BuildingPlacementDto>> GetBuildingsAsync(Guid userId)
    {
        return await _db.BuildingPlacements
            .Where(b => b.UserId == userId)
            .Select(b => new BuildingPlacementDto
            {
                Id = b.Id,
                X = b.X,
                Y = b.Y,
                BuildingType = b.BuildingType
            })
            .ToListAsync();
    }
    public async Task<BuildingPlacementDto> PlaceBuildingAsync(
    Guid userId,
    PlaceBuildingRequest request)
    {
    if (request.X < 0 || request.X > 2) //holder ting på X aksen
        throw new ArgumentException("X skal være mellem 0 og 2.");

    if (request.Y < 0 || request.Y > 2) //holder ting på Y aksen
        throw new ArgumentException("Y skal være mellem 0 og 2.");

    if (string.IsNullOrWhiteSpace(request.BuildingType))
        throw new ArgumentException("BuildingType mangler.");

    bool occupied = await _db.BuildingPlacements
        .AnyAsync(b =>
            b.UserId == userId &&
            b.X == request.X &&
            b.Y == request.Y);

    if (occupied)
        throw new InvalidOperationException("Denne grid-position er allerede optaget.");

    var building = new BuildingPlacement
    {
        UserId = userId,
        X = request.X,
        Y = request.Y,
        BuildingType = request.BuildingType
    };

    _db.BuildingPlacements.Add(building);

    await _db.SaveChangesAsync();

    return new BuildingPlacementDto
    {
        Id = building.Id,
        X = building.X,
        Y = building.Y,
        BuildingType = building.BuildingType
    };


    
    }
    public async Task<bool> RemoveBuildingAsync(Guid userId, int x, int y)
    {
    var building = await _db.BuildingPlacements
        .FirstOrDefaultAsync(b =>
            b.UserId == userId &&
            b.X == x &&
            b.Y == y);

    if (building == null)
        return false;

    _db.BuildingPlacements.Remove(building);

    await _db.SaveChangesAsync();

    return true;
    }
}

