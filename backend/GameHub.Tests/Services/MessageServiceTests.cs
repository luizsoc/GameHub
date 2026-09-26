using FluentAssertions;
using GameHub.Application.DTOs.Messages;
using GameHub.Application.Interfaces;
using GameHub.Application.Services;
using GameHub.Domain.Entities;
using Moq;

namespace GameHub.Tests.Services;

public class MessageServiceTests
{
    private readonly Mock<IMessageRepository> _messageRepositoryMock = new();
    private readonly Mock<IChannelRepository> _channelRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private MessageService CreateService()
    {
        return new MessageService(
            _messageRepositoryMock.Object,
            _channelRepositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task SendAsync_ShouldCreateMessage()
    {
        var userId = Guid.NewGuid();
        var channelId = Guid.NewGuid();

        var channel = new Channel
        {
            Id = channelId,
            Name = "general"
        };

        var request = new SendMessageRequest
        {
            ChannelId = channelId,
            Content = "Hello GameHub!"
        };

        _channelRepositoryMock
            .Setup(x => x.GetAccessibleByIdAsync(channelId, It.IsAny<Guid>()))
            .ReturnsAsync(channel);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Username = "luiz" });

        var service = CreateService();

        var result = await service.SendAsync(userId, request);

        result.Content.Should().Be("Hello GameHub!");
        result.UserId.Should().Be(userId);
        result.Username.Should().Be("luiz");
        result.ChannelId.Should().Be(channelId);

        _messageRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Message>(message =>
                message.UserId == userId &&
                message.ChannelId == channelId &&
                message.Content == "Hello GameHub!")),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldTrimMessageContent()
    {
        var userId = Guid.NewGuid();
        var channelId = Guid.NewGuid();

        var channel = new Channel
        {
            Id = channelId,
            Name = "general"
        };

        var request = new SendMessageRequest
        {
            ChannelId = channelId,
            Content = "   Hello GameHub!   "
        };

        _channelRepositoryMock
            .Setup(x => x.GetAccessibleByIdAsync(channelId, It.IsAny<Guid>()))
            .ReturnsAsync(channel);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Username = "luiz" });

        var service = CreateService();

        var result = await service.SendAsync(userId, request);

        result.Content.Should().Be("Hello GameHub!");
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenContentIsEmpty()
    {
        var request = new SendMessageRequest
        {
            ChannelId = Guid.NewGuid(),
            Content = ""
        };

        var service = CreateService();

        var act = () => service.SendAsync(
            Guid.NewGuid(),
            request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Message content is required.");
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenChannelIdIsEmpty()
    {
        var request = new SendMessageRequest
        {
            ChannelId = Guid.Empty,
            Content = "Hello"
        };

        var service = CreateService();

        var act = () => service.SendAsync(
            Guid.NewGuid(),
            request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("ChannelId is required.");
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenChannelDoesNotExist()
    {
        var channelId = Guid.NewGuid();

        var request = new SendMessageRequest
        {
            ChannelId = channelId,
            Content = "Hello"
        };

        _channelRepositoryMock
            .Setup(x => x.GetAccessibleByIdAsync(channelId, It.IsAny<Guid>()))
            .ReturnsAsync((Channel?)null);

        var service = CreateService();

        var act = () => service.SendAsync(
            Guid.NewGuid(),
            request);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Channel not found.");
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var channelId = Guid.NewGuid();

        var request = new SendMessageRequest
        {
            ChannelId = channelId,
            Content = "Hello"
        };

        _channelRepositoryMock
            .Setup(x => x.GetAccessibleByIdAsync(channelId, It.IsAny<Guid>()))
            .ReturnsAsync(new Channel { Id = channelId, Name = "general" });

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        var service = CreateService();

        var act = () => service.SendAsync(userId, request);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found.");

        _messageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Message>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByChannelAsync_ShouldReturnMessages()
    {
        var channelId = Guid.NewGuid();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "luiz"
        };

        var messages = new List<Message>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Content = "Hello",
                UserId = user.Id,
                User = user,
                ChannelId = channelId
            },
            new()
            {
                Id = Guid.NewGuid(),
                Content = "World",
                UserId = user.Id,
                User = user,
                ChannelId = channelId
            }
        };

        _channelRepositoryMock
            .Setup(x => x.GetAccessibleByIdAsync(channelId, user.Id))
            .ReturnsAsync(new Channel { Id = channelId, Name = "general" });

        _messageRepositoryMock
            .Setup(x => x.GetByChannelAsync(channelId))
            .ReturnsAsync(messages);

        var service = CreateService();

        var result = await service.GetByChannelAsync(user.Id, channelId);

        result.Should().HaveCount(2);
        result.First().Username.Should().Be("luiz");
        result.First().Content.Should().Be("Hello");
        result.Last().Content.Should().Be("World");
    }

    [Fact]
    public async Task GetByChannelAsync_ShouldThrow_WhenChannelIdIsEmpty()
    {
        var service = CreateService();

        var act = () => service.GetByChannelAsync(Guid.NewGuid(), Guid.Empty);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("ChannelId is required.");
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenContentIsTooLong()
    {
        var request = new SendMessageRequest
        {
            ChannelId = Guid.NewGuid(),
            Content = new string('a', Message.ContentMaxLength + 1)
        };

        var service = CreateService();

        var act = () => service.SendAsync(
            Guid.NewGuid(),
            request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage($"Message content must be at most {Message.ContentMaxLength} characters.");

        _messageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Message>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByChannelAsync_ShouldThrow_WhenChannelIsNotAccessible()
    {
        // A private channel the user is not a member of: the repository's
        // access rule returns nothing for this user.
        var userId = Guid.NewGuid();
        var channelId = Guid.NewGuid();

        _channelRepositoryMock
            .Setup(x => x.GetAccessibleByIdAsync(channelId, userId))
            .ReturnsAsync((Channel?)null);

        var service = CreateService();

        var act = () => service.GetByChannelAsync(userId, channelId);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Channel not found.");

        _messageRepositoryMock.Verify(
            x => x.GetByChannelAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_ShouldNotStoreMessage_WhenChannelIsNotAccessible()
    {
        var userId = Guid.NewGuid();
        var channelId = Guid.NewGuid();

        var request = new SendMessageRequest
        {
            ChannelId = channelId,
            Content = "Hello"
        };

        _channelRepositoryMock
            .Setup(x => x.GetAccessibleByIdAsync(channelId, userId))
            .ReturnsAsync((Channel?)null);

        var service = CreateService();

        var act = () => service.SendAsync(userId, request);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Channel not found.");

        // Checked for this user specifically, and nothing was stored.
        _channelRepositoryMock.Verify(
            x => x.GetAccessibleByIdAsync(channelId, userId),
            Times.Once);

        _messageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Message>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}