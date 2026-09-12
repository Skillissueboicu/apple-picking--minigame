using static BCrypt.Net.BCrypt;

using FarmerQuest.Server.Security;
using FarmerQuest.Server.Models;
using FarmerQuest.Server.Features.Auth.DTOs;
using FarmerQuest.Server.Features.Users;

namespace FarmerQuest.Server.Features.Auth;

/// <summary>
/// Handles user registration, login, and JWT issuance
/// </summary>
public class AuthService(IUserRepo userRepo, IJwtTokenFactory jwt)
{
    private readonly IUserRepo _userRepo = userRepo;
    private readonly IJwtTokenFactory _jwt = jwt;

    /// <summary>
    /// Creates a new user (first user becomes SuperAdmin) and returns a token.
    /// Returns null if the email is already registered
    /// </summary>
    public async Task<PostAuthResponse?> RegisterAsync(PostRegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Reject duplicate emails early
        if (await _userRepo.GetByEmailAsync(request.Email, cancellationToken) != null)
            return null;

        // Bootstrap: empty database --> SuperAdmin; otherwise standard User
        string role = await _userRepo.AnyUsersExistAsync(cancellationToken) ? AppRoles.User : AppRoles.SuperAdmin;

        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = HashPassword(request.Password),
            Role = role,
        };

        await _userRepo.AddAsync(user, cancellationToken);

        return new PostAuthResponse
        {
            Token = _jwt.CreateAccessToken(user),
            User = ToUserInfo(user),
        };
    }

    /// <summary>
    /// Verifies credentials and returns a token, or null on failure
    /// </summary>
    public async Task<PostAuthResponse?> LoginAsync(PostLoginRequest request, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepo.GetByEmailAsync(request.Email, cancellationToken);
        // Missing user or empty hash --> failed login
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return null;

        if (!Verify(request.Password, user.PasswordHash))
            return null;

        return new PostAuthResponse
        {
            Token = _jwt.CreateAccessToken(user),
            User = ToUserInfo(user),
        };
    }

    /// <summary>
    /// Maps a User entity to the public auth profile DTO
    /// </summary>
    private static GetUserSimpleResponse ToUserInfo(User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
        };
}
