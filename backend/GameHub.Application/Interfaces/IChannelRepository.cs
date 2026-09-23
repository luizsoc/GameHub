using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces;

public interface IChannelRepository
{
    Task AddAsync(Channel channel);

    Task<IEnumerable<Channel>> GetAllAsync();

    Task<Channel?> GetByIdAsync(Guid id);

    Task<bool> ExistsByNameAsync(string name);
}