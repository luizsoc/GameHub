using GameHub.Application.DTOs.Messages;

namespace GameHub.Application.Interfaces;

public interface IMessageService
{
    Task<MessageResponse> SendAsync(
        Guid userId,
        SendMessageRequest request);

    Task<IEnumerable<MessageResponse>> GetByChannelAsync(
        Guid userId,
        Guid channelId);
}