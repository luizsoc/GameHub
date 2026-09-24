using GameHub.Application.DTOs.Messages;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;

namespace GameHub.Application.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MessageService(
        IMessageRepository messageRepository,
        IChannelRepository channelRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _channelRepository = channelRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MessageResponse> SendAsync(
        Guid userId,
        SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException(
                "Message content is required.");
        }

        if (request.ChannelId == Guid.Empty)
        {
            throw new ArgumentException(
                "ChannelId is required.");
        }

        var channel = await _channelRepository
            .GetByIdAsync(request.ChannelId);

        if (channel is null)
        {
            throw new KeyNotFoundException(
                "Channel not found.");
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "User not found.");
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = request.Content.Trim(),
            UserId = userId,
            ChannelId = request.ChannelId
        };

        await _messageRepository.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        return new MessageResponse
        {
            Id = message.Id,
            Content = message.Content,
            UserId = message.UserId,
            Username = user.Username,
            ChannelId = message.ChannelId,
            CreatedAt = message.CreatedAt
        };
    }

    public async Task<IEnumerable<MessageResponse>> GetByChannelAsync(
        Guid channelId)
    {
        if (channelId == Guid.Empty)
        {
            throw new ArgumentException(
                "ChannelId is required.");
        }

        var messages = await _messageRepository
            .GetByChannelAsync(channelId);

        return messages.Select(message => new MessageResponse
        {
            Id = message.Id,
            Content = message.Content,
            UserId = message.UserId,
            Username = message.User.Username,
            ChannelId = message.ChannelId,
            CreatedAt = message.CreatedAt
        });
    }
}