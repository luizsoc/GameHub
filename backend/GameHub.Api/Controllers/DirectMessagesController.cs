using System.Security.Claims;
using GameHub.Api.Hubs;
using GameHub.Application.DTOs.Channels;
using GameHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GameHub.Api.Controllers;

// A direct message is a private channel with two members: its history,
// sending and SignalR group use the regular endpoints with its id.
[Authorize]
[ApiController]
[Route("api/direct-messages")]
public class DirectMessagesController : ControllerBase
{
    private readonly IDirectMessageService _directMessageService;
    private readonly IHubContext<ChatHub> _hubContext;

    public DirectMessagesController(
        IDirectMessageService directMessageService,
        IHubContext<ChatHub> hubContext)
    {
        _directMessageService = directMessageService;
        _hubContext = hubContext;
    }

    // The user's direct messages, each one showing the other participant.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DirectMessageResponse>>> GetAll()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        var directMessages = await _directMessageService.GetAllAsync(userId);

        return Ok(directMessages);
    }

    // Opens the conversation with another user: 200 with the existing one,
    // or 201 when this call created it.
    [HttpPost]
    public async Task<ActionResult<DirectMessageResponse>> Open(
        OpenDirectMessageRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        try
        {
            var result = await _directMessageService.OpenAsync(userId, request);

            if (!result.Created)
            {
                return Ok(result.ForCaller);
            }

            // Both participants (all their open sessions) get the new
            // conversation in their list, each from their own point of view.
            // Users are addressed by the JWT "sub" (SubClaimUserIdProvider);
            // nobody else is notified.
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync(ChatHub.DirectMessageCreatedEvent, result.ForCaller);

            await _hubContext.Clients
                .User(result.ForCaller.UserId.ToString())
                .SendAsync(ChatHub.DirectMessageCreatedEvent, result.ForRecipient);

            return Created($"/api/channels/{result.ForCaller.Id}", result.ForCaller);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue("sub"), out userId);
    }
}
