using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FarmerQuest.Server.Features.Auth.DTOs;

namespace FarmerQuest.Server.Features.Auth;

/// <summary>
/// HTTP endpoints for registration and login
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user and returns a JWT plus basic user info.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<PostAuthResponse>> Register(
        [FromBody] PostRegisterRequest? request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { error = "Request body mangler" });

        PostAuthResponse? result = await _authService.RegisterAsync(request, cancellationToken);
        if (result == null)
            return BadRequest(new { error = "Email findes allerede" });

        return Created("/auth/login", result);
    }

    /// <summary>
    /// Authenticates with email/password and returns a JWT plus basic user info.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<PostAuthResponse>> Login(
        [FromBody] PostLoginRequest? request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { error = "Request body mangler." });

        PostAuthResponse? result = await _authService.LoginAsync(request, cancellationToken);
        if (result == null)
        {
            // Same message for missing email or wrong password (avoids user enumeration)
            return Unauthorized(new { error = "Forkert email eller adgangskode." });
        }

        return Ok(result);
    }
}
