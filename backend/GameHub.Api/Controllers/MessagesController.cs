using System.Security.Claims;
using GameHub.Application.DTOs.Messages;
using GameHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Send(
        SendMessageRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var message = await _messageService.SendAsync(
                userId,
                request);

            return Ok(message);
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

    [HttpGet("channel/{channelId:guid}")]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetByChannel(
        Guid channelId)
    {
        try
        {
            var messages = await _messageService
                .GetByChannelAsync(channelId);

            return Ok(messages);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}