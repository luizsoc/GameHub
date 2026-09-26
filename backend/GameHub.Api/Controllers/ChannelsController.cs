using System.Security.Claims;
using GameHub.Application.DTOs.Channels;
using GameHub.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GameHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChannelsController : ControllerBase
{
    private readonly IChannelService _channelService;

    public ChannelsController(IChannelService channelService)
    {
        _channelService = channelService;
    }

    // Public channels plus the private channels the user is a member of.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChannelResponse>>> GetAll()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        var channels = await _channelService.GetAllAsync(userId);

        return Ok(channels);
    }

    // 404 both for a channel that does not exist and for a private channel
    // the user is not a member of.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChannelResponse>> GetById(Guid id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        var channel = await _channelService.GetByIdAsync(userId, id);

        if (channel is null)
        {
            return NotFound();
        }

        return Ok(channel);
    }

    [HttpPost]
    public async Task<ActionResult<ChannelResponse>> Create(
        CreateChannelRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        try
        {
            var channel = await _channelService.CreateAsync(userId, request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = channel.Id },
                channel);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // Adds a user (by username) to a private channel. Only members of the
    // channel can do it.
    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ChannelMemberResponse>> AddMember(
        Guid id,
        AddChannelMemberRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        try
        {
            var member = await _channelService.AddMemberAsync(
                userId,
                id,
                request);

            return Ok(member);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue("sub"), out userId);
    }
}
