using GameHub.Application.DTOs.Channels;

namespace GameHub.Application.Interfaces;

public interface IChannelService
{
    Task<ChannelResponse> CreateAsync(CreateChannelRequest request);

    Task<IEnumerable<ChannelResponse>> GetAllAsync();

    Task<ChannelResponse?> GetByIdAsync(Guid id);
}