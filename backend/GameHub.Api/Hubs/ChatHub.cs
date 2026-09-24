using GameHub.Application.DTOs.Messages;
using GameHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameHub.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;

    public ChatHub(IMessageService messageService)
    {
        _messageService = messageService;
    }

    public async Task SendMessage(SendMessageRequest request)
    {
        var userId = Context.UserIdentifier;

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new HubException("Invalid user identity.");
        }

        var message = await _messageService.SendAsync(
            parsedUserId,
            request);

        await Clients.Group(request.ChannelId.ToString())
            .SendAsync("ReceiveMessage", message);
    }
    public async Task JoinChannel(Guid channelId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            channelId.ToString());
    }
    
    public async Task LeaveChannel(Guid channelId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            channelId.ToString());
    }
}