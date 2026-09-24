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
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private MessageService CreateService()
    {
        return new MessageService(
            _messageRepositoryMock.Object,
            _channelRepositoryMock.Object,
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
            .Setup(x => x.GetByIdAsync(channelId))
            .ReturnsAsync(channel);

        var service = CreateService();

        var result = await service.SendAsync(userId, request);

        result.Content.Should().Be("Hello GameHub!");
        result.UserId.Should().Be(userId);
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
            .Setup(x => x.GetByIdAsync(channelId))
            .ReturnsAsync(channel);

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
            .Setup(x => x.GetByIdAsync(channelId))
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

        _messageRepositoryMock
            .Setup(x => x.GetByChannelAsync(channelId))
            .ReturnsAsync(messages);

        var service = CreateService();

        var result = await service.GetByChannelAsync(channelId);

        result.Should().HaveCount(2);
        result.First().Username.Should().Be("luiz");
        result.First().Content.Should().Be("Hello");
        result.Last().Content.Should().Be("World");
    }

    [Fact]
    public async Task GetByChannelAsync_ShouldThrow_WhenChannelIdIsEmpty()
    {
        var service = CreateService();

        var act = () => service.GetByChannelAsync(Guid.Empty);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("ChannelId is required.");
    }
}