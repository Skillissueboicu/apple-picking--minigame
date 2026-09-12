using FarmerQuest.Server.Models;

namespace FarmerQuest.Server.Features.Users;

// Persistence contract for User entities
public interface IUserRepo
{
    // Returns true if at least one user exists
    Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken = default);

    // Counts users with the given role
    Task<int> CountByRoleAsync(string role, CancellationToken cancellationToken = default);

    // Lists all users (no tracking)
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);

    // Loads a user by id, or null if missing
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    // Loads a user by email, or null if missing
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    // Inserts a new user (assigns id/timestamps and default PlayerStat)
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    // Updates an existing user; returns false if not found
    Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default);

    // Deletes a user by id; returns false if not found
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
