using static BCrypt.Net.BCrypt;

using FarmerQuest.Server.Features.Users.DTOs;
using FarmerQuest.Server.Models;
using FarmerQuest.Server.Security;

namespace FarmerQuest.Server.Features.Users;

/// <summary>
/// Business logic for user CRUD, role changes, and mapping to API DTOs
/// </summary>
public class UserService
{
    private readonly IUserRepo _userRepo;

    public UserService(IUserRepo userRepo)
    {
        _userRepo = userRepo;
    }

    // Returns all users as API responses
    public async Task<List<GetUserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepo.GetAllAsync(cancellationToken);
        return users.ConvertAll(MapToResponse);
    }

    // Returns one user by id, or null if missing
    public async Task<GetUserResponse?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepo.GetByIdAsync(id, cancellationToken);
        return user != null ? MapToResponse(user) : null;
    }

    // Returns one user by email, or null if missing
    public async Task<GetUserResponse?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepo.GetByEmailAsync(email, cancellationToken);
        return user != null ? MapToResponse(user) : null;
    }

    // Creates a user with an empty password hash and the default User role
    public async Task<GetUserResponse> CreateUserAsync(PostUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = string.Empty,
            Role = AppRoles.User,
        };

        User createdUser = await _userRepo.AddAsync(user, cancellationToken);
        return MapToResponse(createdUser);
    }

    // Full replace update of email/name (optional password, role, team)
    public async Task<GetUserResponse?> PutUserAsync(string id, PutUserRequest request, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepo.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return null;

        user.Email = request.Email;
        user.Name = request.Name;
        user.UpdatedAt = DateTime.UtcNow;

        // Hash only when a new password was supplied
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = HashPassword(request.Password);

        if (request.Role != null)
        {
            if (!AppRoles.IsValid(request.Role))
                throw new InvalidOperationException("Ugyldig rolle.");
            user.Role = request.Role;
        }

        if (request.Team != null)
            user.Team = request.Team.Trim();

        bool updated = await _userRepo.UpdateAsync(user, cancellationToken);
        return updated ? MapToResponse(user) : null;
    }

    // Partial update: only non-null fields are applied
    public async Task<GetUserResponse?> PatchUserAsync(string id, PatchUserRequest request, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepo.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return null;

        // Apply only provided fields
        if (request.Email != null)
            user.Email = request.Email;
        if (request.Name != null)
            user.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = HashPassword(request.Password);
        if (request.Role != null)
        {
            if (!AppRoles.IsValid(request.Role))
                throw new InvalidOperationException("Ugyldig rolle.");
            user.Role = request.Role;
        }

        if (request.Team != null)
            user.Team = request.Team.Trim();

        user.UpdatedAt = DateTime.UtcNow;

        bool updated = await _userRepo.UpdateAsync(user, cancellationToken);
        return updated ? MapToResponse(user) : null;
    }

    // Deletes a user. SuperAdmin targets require a SuperAdmin actor; the last SuperAdmin cannot be deleted
    public async Task<bool> DeleteUserAsync(
        string id,
        string actorRole,
        DeleteUserRequest? deleteRequest = null,
        CancellationToken cancellationToken = default)
    {
        User? target = await _userRepo.GetByIdAsync(id, cancellationToken);
        if (target == null)
            return false;

        // Protect SuperAdmin accounts and the last SuperAdmin
        if (target.Role == AppRoles.SuperAdmin)
        {
            if (actorRole != AppRoles.SuperAdmin)
                throw new InvalidOperationException("Kun SuperAdmin kan slette en SuperAdmin.");

            int superAdminCount = await _userRepo.CountByRoleAsync(AppRoles.SuperAdmin, cancellationToken);
            if (superAdminCount <= 1)
                throw new InvalidOperationException("Sidste SuperAdmin kan ikke slettes.");
        }

        return await _userRepo.DeleteAsync(id, cancellationToken);
    }

    // Sets a user's role. The last SuperAdmin cannot be demoted
    public async Task<GetUserResponse?> ChangeUserRoleAsync(
        string id,
        string newRole,
        CancellationToken cancellationToken = default)
    {
        if (!AppRoles.IsValid(newRole))
            throw new InvalidOperationException("Ugyldig rolle.");

        User? user = await _userRepo.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return null;

        // Block demoting the only remaining SuperAdmin
        if (user.Role == AppRoles.SuperAdmin && newRole != AppRoles.SuperAdmin)
        {
            int superAdminCount = await _userRepo.CountByRoleAsync(AppRoles.SuperAdmin, cancellationToken);
            if (superAdminCount <= 1)
                throw new InvalidOperationException("Sidste SuperAdmin kan ikke nedgraderes.");
        }

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;

        bool updated = await _userRepo.UpdateAsync(user, cancellationToken);
        return updated ? MapToResponse(user) : null;
    }

    // Maps a User entity to the public API profile
    private static GetUserResponse MapToResponse(User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            Team = user.Team ?? string.Empty,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
        };
}
