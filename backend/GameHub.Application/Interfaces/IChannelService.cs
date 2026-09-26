using GameHub.Application.DTOs.Channels;

namespace GameHub.Application.Interfaces;

public interface IChannelService
{
    Task<ChannelResponse> CreateAsync(Guid userId, CreateChannelRequest request);

    Task<IEnumerable<ChannelResponse>> GetAllAsync(Guid userId);

    // Null when the channel does not exist or is private and the user is not
    // a member (the two cases are indistinguishable on purpose).
    Task<ChannelResponse?> GetByIdAsync(Guid userId, Guid id);

    Task<ChannelMemberResponse> AddMemberAsync(
        Guid userId,
        Guid channelId,
        AddChannelMemberRequest request);
}
