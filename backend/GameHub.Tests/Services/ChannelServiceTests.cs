using FluentAssertions;
using GameHub.Application.DTOs.Channels;
using GameHub.Application.Interfaces;
using GameHub.Application.Services;
using GameHub.Domain.Entities;
using Moq;

namespace GameHub.Tests.Services;

public class ChannelServiceTests
{
    private readonly Mock<IChannelRepository> _channelRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly ChannelService _service;

    public ChannelServiceTests()
    {
        _channelRepository = new Mock<IChannelRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _service = new ChannelService(
            _channelRepository.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateChannel()
    {
        // Arrange
        var request = new CreateChannelRequest
        {
            Name = "League of Legends",
            Description = "League of Legends community"
        };

        _channelRepository
            .Setup(x => x.ExistsByNameAsync(request.Name))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("League of Legends");
        result.Description.Should().Be("League of Legends community");

        _channelRepository.Verify(
            x => x.AddAsync(It.IsAny<Channel>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNameAlreadyExists()
    {
        // Arrange
        var request = new CreateChannelRequest
        {
            Name = "League of Legends"
        };

        _channelRepository
            .Setup(x => x.ExistsByNameAsync(request.Name))
            .ReturnsAsync(true);

        // Act
        var action = async () => await _service.CreateAsync(request);

        // Assert
        await action.Should()
            .ThrowAsync<InvalidOperationException>();

        _channelRepository.Verify(
            x => x.AddAsync(It.IsAny<Channel>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateChannelRequest
        {
            Name = "   "
        };

        // Act
        var action = async () => await _service.CreateAsync(request);

        // Assert
        await action.Should()
            .ThrowAsync<ArgumentException>();

        _channelRepository.Verify(
            x => x.AddAsync(It.IsAny<Channel>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnChannel_WhenChannelExists()
    {
        // Arrange
        var channelId = Guid.NewGuid();

        var channel = new Channel
        {
            Id = channelId,
            Name = "Counter-Strike",
            Description = "CS community"
        };

        _channelRepository
            .Setup(x => x.GetByIdAsync(channelId))
            .ReturnsAsync(channel);

        // Act
        var result = await _service.GetByIdAsync(channelId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(channelId);
        result.Name.Should().Be("Counter-Strike");
        result.Description.Should().Be("CS community");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenChannelDoesNotExist()
    {
        // Arrange
        var channelId = Guid.NewGuid();

        _channelRepository
            .Setup(x => x.GetByIdAsync(channelId))
            .ReturnsAsync((Channel?)null);

        // Act
        var result = await _service.GetByIdAsync(channelId);

        // Assert
        result.Should().BeNull();
    }
}