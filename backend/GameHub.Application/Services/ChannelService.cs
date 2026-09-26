using GameHub.Application.DTOs.Channels;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;

namespace GameHub.Application.Services;

public class ChannelService : IChannelService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChannelService(
        IChannelRepository channelRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _channelRepository = channelRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChannelResponse> CreateAsync(
        Guid userId,
        CreateChannelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Channel name is required.");
        }

        var name = request.Name.Trim();
        var description = request.Description?.Trim();

        // Checked here so an oversized value is a 400, not a database error.
        if (name.Length > Channel.NameMaxLength)
        {
            throw new ArgumentException(
                $"Channel name must be at most {Channel.NameMaxLength} characters.");
        }

        if (description?.Length > Channel.DescriptionMaxLength)
        {
            throw new ArgumentException(
                $"Channel description must be at most {Channel.DescriptionMaxLength} characters.");
        }

        if (name.StartsWith(Channel.DirectMessageNamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Channel names cannot start with \"{Channel.DirectMessageNamePrefix}\".");
        }

        if (await _channelRepository.ExistsByNameAsync(name))
        {
            throw new InvalidOperationException(
                "A channel with this name already exists.");
        }

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsPrivate = request.IsPrivate
        };

        // The creator of a private channel is its first member. Added to the
        // channel itself, so both rows are inserted by the same SaveChanges
        // (one transaction): a private channel never exists without a member.
        if (channel.IsPrivate)
        {
            channel.Members.Add(new ChannelMember
            {
                UserId = userId,
                ChannelId = channel.Id
            });
        }

        await _channelRepository.AddAsync(channel);
        await _unitOfWork.SaveChangesAsync();

        return ToResponse(channel);
    }

    public async Task<IEnumerable<ChannelResponse>> GetAllAsync(Guid userId)
    {
        var channels = await _channelRepository.GetAccessibleAsync(userId);

        return channels.Select(ToResponse);
    }

    public async Task<ChannelResponse?> GetByIdAsync(Guid userId, Guid id)
    {
        var channel = await _channelRepository.GetAccessibleByIdAsync(id, userId);

        if (channel is null)
        {
            return null;
        }

        return ToResponse(channel);
    }

    public async Task<ChannelMemberResponse> AddMemberAsync(
        Guid userId,
        Guid channelId,
        AddChannelMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("Username is required.");
        }

        // Only someone who can access the channel may add members; for a
        // private channel that means being a member. Anyone else gets the
        // same answer as for a channel that does not exist, before the
        // username is even looked up.
        var channel = await _channelRepository
            .GetAccessibleByIdAsync(channelId, userId);

        if (channel is null)
        {
            throw new KeyNotFoundException("Channel not found.");
        }

        // Public channels are open to everyone: there is no member list.
        if (!channel.IsPrivate)
        {
            throw new ArgumentException(
                "Members can only be added to private channels.");
        }

        // A direct message is between its two participants only.
        if (channel.IsDirectMessage)
        {
            throw new ArgumentException(
                "Members cannot be added to a direct message.");
        }

        var user = await _userRepository
            .GetByUsernameAsync(request.Username.Trim());

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        // Also covers a member adding themselves.
        if (await _channelRepository.IsMemberAsync(channel.Id, user.Id))
        {
            throw new InvalidOperationException(
                "User is already a member of this channel.");
        }

        var member = new ChannelMember
        {
            UserId = user.Id,
            ChannelId = channel.Id
        };

        await _channelRepository.AddMemberAsync(member);
        await _unitOfWork.SaveChangesAsync();

        return new ChannelMemberResponse
        {
            UserId = user.Id,
            Username = user.Username,
            ChannelId = channel.Id,
            JoinedAt = member.JoinedAt
        };
    }

    private static ChannelResponse ToResponse(Channel channel)
    {
        return new ChannelResponse
        {
            Id = channel.Id,
            Name = channel.Name,
            Description = channel.Description,
            IsPrivate = channel.IsPrivate,
            CreatedAt = channel.CreatedAt
        };
    }
}
