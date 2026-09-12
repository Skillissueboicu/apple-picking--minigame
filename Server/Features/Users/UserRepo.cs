using Microsoft.EntityFrameworkCore;

using FarmerQuest.Server.Databases;
using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.Features.Users;

// EF Core implementation of IUserRepo
public class UserRepo : IUserRepo
{
    private readonly FarmerQuestDbContext _dbContext;

    public UserRepo(FarmerQuestDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Returns true if the Users table has any rows
    public Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(cancellationToken);

    // Counts users assigned to the given role
    public Task<int> CountByRoleAsync(string role, CancellationToken cancellationToken = default) =>
        _dbContext.Users.CountAsync(u => u.Role == role, cancellationToken);

    // Inserts a user with generated id, timestamps, and a default PlayerStat
    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(user.Id))
            user.Id = Guid.NewGuid().ToString();

        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        // PlayerStat only here - badges are seeded later in PlayerStatService (two-step insert)
        user.PlayerStat = PlayerStat.CreateDefaultForUser(user.Id);
        user.PlayerStat.User = user;

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    // Deletes the user with the given id; returns false if not found
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        User? existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (existingUser == null)
            return false;

        _dbContext.Users.Remove(existingUser);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Returns all users without change tracking
    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    // Finds a user by email, or null if none match
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    // Finds a user by id, or null if none match
    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    // Copies mutable fields onto the tracked entity and saves; returns false if missing
    public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        User? existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
        if (existingUser == null)
            return false;

        // Overwrite tracked entity fields from the incoming model
        existingUser.Email = user.Email;
        existingUser.Name = user.Name;
        existingUser.Role = user.Role;
        existingUser.Team = user.Team;
        existingUser.PasswordHash = user.PasswordHash;
        existingUser.UpdatedAt = user.UpdatedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
