using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces;

public interface IMessageRepository
{
    Task AddAsync(Message message);

    Task<IEnumerable<Message>> GetByChannelAsync(Guid channelId);
}