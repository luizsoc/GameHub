using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces;

public interface IChannelRepository
{
    Task AddAsync(Channel channel);

    // Channel reads always go through the access rule: a channel is
    // accessible to a user when it is public or the user is in its members.
    // A private channel the user is not a member of behaves as if it did not
    // exist. Direct messages are channels too (private, two members), so the
    // same rule protects them.
    //
    // Regular channels only: direct messages have their own list.
    Task<IEnumerable<Channel>> GetAccessibleAsync(Guid userId);

    Task<Channel?> GetAccessibleByIdAsync(Guid id, Guid userId);

    Task<bool> ExistsByNameAsync(string name);

    Task<bool> IsMemberAsync(Guid channelId, Guid userId);

    Task AddMemberAsync(ChannelMember member);

    // With members and their users, to name the other participant.
    Task<Channel?> GetDirectMessageAsync(string directMessageKey);

    Task<IEnumerable<Channel>> GetDirectMessagesAsync(Guid userId);
}
