using GameHub.Application.DTOs.Channels;
using GameHub.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelsController : ControllerBase
{
    private readonly IChannelService _channelService;

    public ChannelsController(IChannelService channelService)
    {
        _channelService = channelService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChannelResponse>>> GetAll()
    {
        var channels = await _channelService.GetAllAsync();

        return Ok(channels);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChannelResponse>> GetById(Guid id)
    {
        var channel = await _channelService.GetByIdAsync(id);

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
        try
        {
            var channel = await _channelService.CreateAsync(request);

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
}