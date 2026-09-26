using GameHub.Application.DTOs.Channels;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;

namespace GameHub.Application.Services;

// Direct messages are private channels with exactly two members and a
// DirectMessageKey. Everything else (history, sending, SignalR groups and the
// access rule) is the regular channel infrastructure.
public class DirectMessageService : IDirectMessageService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DirectMessageService(
        IChannelRepository channelRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _channelRepository = channelRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OpenDirectMessageResult> OpenAsync(
        Guid userId,
        OpenDirectMessageRequest request)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.");
        }

        if (request.UserId == userId)
        {
            throw new ArgumentException(
                "You cannot start a direct message with yourself.");
        }

        var caller = await _userRepository.GetByIdAsync(userId);
        var recipient = await _userRepository.GetByIdAsync(request.UserId);

        if (caller is null || recipient is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var key = Channel.DirectMessageKeyFor(caller.Id, recipient.Id);

        var existing = await _channelRepository.GetDirectMessageAsync(key);

        if (existing is not null)
        {
            return ToResult(existing, caller, recipient, created: false);
        }

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = Channel.DirectMessageNamePrefix + key,
            IsPrivate = true,
            DirectMessageKey = key
        };

        // Both memberships are inserted with the channel, in one transaction.
        channel.Members.Add(new ChannelMember { UserId = caller.Id, ChannelId = channel.Id });
        channel.Members.Add(new ChannelMember { UserId = recipient.Id, ChannelId = channel.Id });

        await _channelRepository.AddAsync(channel);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Another request for the same pair (A → B or B → A) may have
            // created the conversation between our check and our insert; the
            // unique index on DirectMessageKey then rejects this one. If the
            // conversation exists now, use it. Any other failure is rethrown.
            var concurrent = await _channelRepository.GetDirectMessageAsync(key);

            if (concurrent is null)
            {
                throw;
            }

            return ToResult(concurrent, caller, recipient, created: false);
        }

        return ToResult(channel, caller, recipient, created: true);
    }

    public async Task<IEnumerable<DirectMessageResponse>> GetAllAsync(Guid userId)
    {
        var channels = await _channelRepository.GetDirectMessagesAsync(userId);

        return channels
            .Select(channel =>
            {
                var other = channel.Members.First(member => member.UserId != userId).User;

                return ToResponse(channel, other);
            })
            .OrderBy(response => response.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static OpenDirectMessageResult ToResult(
        Channel channel,
        User caller,
        User recipient,
        bool created)
    {
        return new OpenDirectMessageResult
        {
            ForCaller = ToResponse(channel, recipient),
            ForRecipient = ToResponse(channel, caller),
            Created = created
        };
    }

    // "other" is the participant shown to the viewer.
    private static DirectMessageResponse ToResponse(Channel channel, User other)
    {
        return new DirectMessageResponse
        {
            Id = channel.Id,
            UserId = other.Id,
            Username = other.Username,
            CreatedAt = channel.CreatedAt
        };
    }
}
