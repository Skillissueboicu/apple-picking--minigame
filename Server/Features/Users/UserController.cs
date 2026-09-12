using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FarmerQuest.Server.Features.Users.DTOs;
using FarmerQuest.Server.Security;

namespace FarmerQuest.Server.Features.Users;

/// <summary>
/// Authenticated HTTP endpoints for listing, updating, and deleting users
/// </summary>
[ApiController]
[Route("users")]
[Authorize]
public sealed class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }


    // Lists users and admins
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<GetUserResponse>>> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllUsersAsync(cancellationToken);
        // Hide SuperAdmin rows from regular Admins
        bool isSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);
        if (!isSuperAdmin)
            users = users.Where(u => u.Role != AppRoles.SuperAdmin).ToList();

        return Ok(users);
    }

    // Gets one user by id. Self or Admin; SuperAdmin profiles are SuperAdmin-only
    [HttpGet("{id}")]
    public async Task<ActionResult<GetUserResponse>> GetUserById(string id, CancellationToken cancellationToken)
    {
        string? tokenUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);
        bool isAdmin = User.IsInRole(AppRoles.Admin) || isSuperAdmin;

        if (string.IsNullOrEmpty(tokenUserId))
            return Unauthorized();

        // Non-admins may only read their own profile
        if (!isAdmin && tokenUserId != id)
            return Forbid();

        GetUserResponse? user = await _userService.GetUserByIdAsync(id, cancellationToken);
        // Non–SuperAdmins cannot view a SuperAdmin profile
        if (user != null && !isSuperAdmin && user.Role == AppRoles.SuperAdmin)
            return Forbid();

        return user != null ? Ok(user) : NotFound();
    }

    // Looks up a user by email (Admins). SuperAdmin profiles are SuperAdmin-only
    [HttpGet("email/{email}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<GetUserResponse>> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
        bool isSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);
        GetUserResponse? user = await _userService.GetUserByEmailAsync(email, cancellationToken);
        if (user != null && !isSuperAdmin && user.Role == AppRoles.SuperAdmin)
            return Forbid();

        return user != null ? Ok(user) : NotFound();
    }

    // Full replace of a user profile. Self or SuperAdmin; role changes are SuperAdmin-only
    [HttpPut("{id}")]
    public async Task<ActionResult<GetUserResponse>> PutUser(
        string id,
        [FromBody] PutUserRequest request,
        CancellationToken cancellationToken)
    {
        string? tokenUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);

        if (string.IsNullOrEmpty(tokenUserId))
            return Unauthorized();

        bool editingSelf = tokenUserId == id;

        // Admins may only read users; editing others requires SuperAdmin (own profile: any user)
        if (!editingSelf && !isSuperAdmin)
            return Forbid();

        // Role: SuperAdmin only (or the dedicated role endpoint)
        if (!isSuperAdmin)
            request.Role = null;

        // SuperAdmin editing someone else: ensure the target exists first
        if (!editingSelf && isSuperAdmin)
        {
            GetUserResponse? target = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (target == null)
                return NotFound();
        }

        try
        {
            GetUserResponse? user = await _userService.PutUserAsync(id, request, cancellationToken);
            return user != null ? Ok(user) : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Partial update of a user profile. Same authorization rules as PUT
    [HttpPatch("{id}")]
    public async Task<ActionResult<GetUserResponse>> PatchUser(
        string id,
        [FromBody] PatchUserRequest request,
        CancellationToken cancellationToken)
    {
        string? tokenUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);

        if (string.IsNullOrEmpty(tokenUserId))
            return Unauthorized();

        bool editingSelf = tokenUserId == id;

        if (!editingSelf && !isSuperAdmin)
            return Forbid();

        // Strip role unless the caller is SuperAdmin
        if (!isSuperAdmin)
            request.Role = null;

        if (!editingSelf && isSuperAdmin)
        {
            GetUserResponse? target = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (target == null)
                return NotFound();
        }

        try
        {
            GetUserResponse? user = await _userService.PatchUserAsync(id, request, cancellationToken);
            return user != null ? Ok(user) : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Deletes a user (SuperAdmin only). Service enforces last-SuperAdmin rules
    [HttpDelete("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteUser(
        string id,
        [FromBody] DeleteUserRequest? deleteRequest,
        CancellationToken cancellationToken)
    {
        string? actorRole = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(actorRole))
            return Unauthorized();

        try
        {
            bool deleted = await _userService.DeleteUserAsync(id, actorRole, deleteRequest, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Changes a user's role (SuperAdmin only)
    [HttpPut("{id}/role")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<GetUserResponse>> ChangeUserRole(
        string id,
        [FromBody] PutUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            GetUserResponse? user = await _userService.ChangeUserRoleAsync(id, request.Role, cancellationToken);
            return user != null ? Ok(user) : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
