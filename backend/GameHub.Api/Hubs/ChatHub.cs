using GameHub.Application.DTOs.Messages;
using GameHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameHub.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IChannelService _channelService;

    public ChatHub(
        IMessageService messageService,
        IChannelService channelService)
    {
        _messageService = messageService;
        _channelService = channelService;
    }

    // Same access rule as the REST API: the group of a private channel only
    // ever contains connections of its members, so ReceiveMessage never
    // reaches anyone else.
    public async Task JoinChannel(Guid channelId)
    {
        var userId = GetUserId();

        var channel = await _channelService.GetByIdAsync(userId, channelId);

        if (channel is null)
        {
            throw new HubException("Channel not found.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            channelId.ToString());
    }

    // Leaving a group exposes nothing, so it needs no access check.
    public async Task LeaveChannel(Guid channelId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            channelId.ToString());
    }

    public async Task SendMessage(SendMessageRequest request)
    {
        var userId = GetUserId();

        // Checks access before storing anything; the broadcast only happens
        // after the message was stored.
        var message = await _messageService.SendAsync(
            userId,
            request);

        await Clients.Group(request.ChannelId.ToString())
            .SendAsync("ReceiveMessage", message);
    }

    private Guid GetUserId()
    {
        if (!Guid.TryParse(Context.UserIdentifier, out var userId))
        {
            throw new HubException("Invalid user identity.");
        }

        return userId;
    }
}
