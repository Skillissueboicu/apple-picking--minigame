using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FarmerQuest.Server.Features.Social.DTOs;

namespace FarmerQuest.Server.Features.Social;

/// <summary>
/// Authenticated HTTP endpoints for friends, friend requests, and blocks.
/// </summary>
[ApiController]
[Route("social")]
[Authorize]
public sealed class SocialController : ControllerBase
{
    private readonly SocialService _social;

    public SocialController(SocialService social)
    {
        _social = social;
    }

    // Current user id from the JWT NameIdentifier claim 
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Lists the caller's friends
    [HttpGet("friends")]
    public async Task<ActionResult<List<FriendResponse>>> GetFriends(CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        return Ok(await _social.GetFriendsAsync(UserId, ct));
    }

    // Lists pending friend requests sent to the caller
    [HttpGet("friend-requests/incoming")]
    public async Task<ActionResult<List<FriendRequestResponse>>> GetIncoming(CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        return Ok(await _social.GetIncomingRequestsAsync(UserId, ct));
    }

    // Lists pending friend requests sent by the caller
    [HttpGet("friend-requests/outgoing")]
    public async Task<ActionResult<List<FriendRequestResponse>>> GetOutgoing(CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        return Ok(await _social.GetOutgoingRequestsAsync(UserId, ct));
    }

    // Sends a friend request by display name (username)
    [HttpPost("friend-requests")]
    public async Task<ActionResult<FriendRequestResponse>> SendFriendRequest(
        [FromBody] SendFriendRequestBody? body, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        if (body == null || string.IsNullOrWhiteSpace(body.Username))
            return BadRequest(new { error = "Brugernavn mangler." });

        (FriendRequestResponse? request, string? error) =
            await _social.SendFriendRequestAsync(UserId, body.Username, ct);
        return request != null ? Ok(request) : BadRequest(new { error });
    }

    // Accepts an incoming friend request and creates the friendship
    [HttpPost("friend-requests/{id:long}/accept")]
    public async Task<IActionResult> AcceptFriendRequest(long id, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (bool ok, string? error) = await _social.AcceptFriendRequestAsync(id, UserId, ct);
        return ok ? NoContent() : BadRequest(new { error });
    }

    // Declines an incoming friend request
    [HttpPost("friend-requests/{id:long}/decline")]
    public async Task<IActionResult> DeclineFriendRequest(long id, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (bool ok, string? error) = await _social.DeclineFriendRequestAsync(id, UserId, ct);
        return ok ? NoContent() : BadRequest(new { error });
    }

    // Removes an existing friendship
    [HttpDelete("friends/{userId}")]
    public async Task<IActionResult> RemoveFriend(string userId, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (bool ok, string? error) = await _social.RemoveFriendAsync(UserId, userId, ct);
        return ok ? NoContent() : BadRequest(new { error });
    }

    // Lists users blocked by the caller
    [HttpGet("blocks")]
    public async Task<ActionResult<List<BlockResponse>>> GetBlocks(CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        return Ok(await _social.GetBlocksAsync(UserId, ct));
    }

    // Blocks a user by display name and cleans up related social state
    [HttpPost("blocks")]
    public async Task<ActionResult<BlockResponse>> BlockUser(
        [FromBody] BlockUserBody? body, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        if (body == null || string.IsNullOrWhiteSpace(body.Username))
            return BadRequest(new { error = "Brugernavn mangler." });

        (BlockResponse? block, string? error) = await _social.BlockUserAsync(UserId, body.Username, ct);
        return block != null ? Ok(block) : BadRequest(new { error });
    }

    // Removes a block for the given user id
    [HttpDelete("blocks/{userId}")]
    public async Task<IActionResult> UnblockUser(string userId, CancellationToken ct)
    {
        if (UserId == null) return Unauthorized();
        (bool ok, string? error) = await _social.UnblockUserAsync(UserId, userId, ct);
        return ok ? NoContent() : BadRequest(new { error });
    }
}
