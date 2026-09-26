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
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly ChannelService _service;

    // The authenticated user making the calls.
    private readonly Guid _userId = Guid.NewGuid();

    public ChannelServiceTests()
    {
        _channelRepository = new Mock<IChannelRepository>();
        _userRepository = new Mock<IUserRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _service = new ChannelService(
            _channelRepository.Object,
            _userRepository.Object,
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
        var result = await _service.CreateAsync(_userId, request);

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
        var action = async () => await _service.CreateAsync(_userId, request);

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
        var action = async () => await _service.CreateAsync(_userId, request);

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
            .Setup(x => x.GetAccessibleByIdAsync(channelId, _userId))
            .ReturnsAsync(channel);

        // Act
        var result = await _service.GetByIdAsync(_userId, channelId);

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
            .Setup(x => x.GetAccessibleByIdAsync(channelId, _userId))
            .ReturnsAsync((Channel?)null);

        // Act
        var result = await _service.GetByIdAsync(_userId, channelId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNameIsTooLong()
    {
        // Arrange
        var request = new CreateChannelRequest
        {
            Name = new string('a', Channel.NameMaxLength + 1)
        };

        // Act
        var action = async () => await _service.CreateAsync(_userId, request);

        // Assert
        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage($"Channel name must be at most {Channel.NameMaxLength} characters.");

        _channelRepository.Verify(
            x => x.AddAsync(It.IsAny<Channel>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenDescriptionIsTooLong()
    {
        // Arrange
        var request = new CreateChannelRequest
        {
            Name = "general",
            Description = new string('a', Channel.DescriptionMaxLength + 1)
        };

        // Act
        var action = async () => await _service.CreateAsync(_userId, request);

        // Assert
        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage($"Channel description must be at most {Channel.DescriptionMaxLength} characters.");

        _channelRepository.Verify(
            x => x.AddAsync(It.IsAny<Channel>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddCreatorAsMember_WhenChannelIsPrivate()
    {
        // Arrange
        var request = new CreateChannelRequest
        {
            Name = "squad",
            IsPrivate = true
        };

        Channel? added = null;

        _channelRepository
            .Setup(x => x.AddAsync(It.IsAny<Channel>()))
            .Callback<Channel>(channel => added = channel);

        // Act
        var result = await _service.CreateAsync(_userId, request);

        // Assert: the membership travels with the channel, so the single
        // SaveChanges below inserts both.
        result.IsPrivate.Should().BeTrue();

        added.Should().NotBeNull();
        added!.IsPrivate.Should().BeTrue();
        added.Members.Should().ContainSingle(member =>
            member.UserId == _userId && member.ChannelId == added.Id);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotAddMembers_WhenChannelIsPublic()
    {
        // Arrange
        var request = new CreateChannelRequest
        {
            Name = "general"
        };

        Channel? added = null;

        _channelRepository
            .Setup(x => x.AddAsync(It.IsAny<Channel>()))
            .Callback<Channel>(channel => added = channel);

        // Act
        var result = await _service.CreateAsync(_userId, request);

        // Assert
        result.IsPrivate.Should().BeFalse();
        added!.IsPrivate.Should().BeFalse();
        added.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnChannelsAccessibleToUser()
    {
        // Arrange
        _channelRepository
            .Setup(x => x.GetAccessibleAsync(_userId))
            .ReturnsAsync(new List<Channel>
            {
                new() { Id = Guid.NewGuid(), Name = "general" },
                new() { Id = Guid.NewGuid(), Name = "squad", IsPrivate = true }
            });

        // Act
        var result = (await _service.GetAllAsync(_userId)).ToList();

        // Assert
        result.Select(x => x.Name).Should().Equal("general", "squad");
        result.Select(x => x.IsPrivate).Should().Equal(false, true);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldAddUser_WhenCallerIsMemberOfPrivateChannel()
    {
        // Arrange
        var channel = PrivateChannelAccessibleToCaller();
        var user = new User { Id = Guid.NewGuid(), Username = "ana" };

        _userRepository
            .Setup(x => x.GetByUsernameAsync("ana"))
            .ReturnsAsync(user);

        _channelRepository
            .Setup(x => x.IsMemberAsync(channel.Id, user.Id))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AddMemberAsync(
            _userId,
            channel.Id,
            new AddChannelMemberRequest { Username = "  ana  " });

        // Assert
        result.UserId.Should().Be(user.Id);
        result.Username.Should().Be("ana");
        result.ChannelId.Should().Be(channel.Id);

        _channelRepository.Verify(
            x => x.AddMemberAsync(It.Is<ChannelMember>(member =>
                member.UserId == user.Id && member.ChannelId == channel.Id)),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenCallerCannotAccessChannel()
    {
        // Arrange: private channel the caller is not a member of.
        var channelId = Guid.NewGuid();

        _channelRepository
            .Setup(x => x.GetAccessibleByIdAsync(channelId, _userId))
            .ReturnsAsync((Channel?)null);

        // Act
        var action = async () => await _service.AddMemberAsync(
            _userId,
            channelId,
            new AddChannelMemberRequest { Username = "ana" });

        // Assert: rejected before the username is even looked up.
        await action.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Channel not found.");

        _userRepository.Verify(
            x => x.GetByUsernameAsync(It.IsAny<string>()),
            Times.Never);

        VerifyNothingAdded();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenChannelIsPublic()
    {
        // Arrange
        var channelId = Guid.NewGuid();

        _channelRepository
            .Setup(x => x.GetAccessibleByIdAsync(channelId, _userId))
            .ReturnsAsync(new Channel { Id = channelId, Name = "general" });

        // Act
        var action = async () => await _service.AddMemberAsync(
            _userId,
            channelId,
            new AddChannelMemberRequest { Username = "ana" });

        // Assert
        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Members can only be added to private channels.");

        VerifyNothingAdded();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenUserDoesNotExist()
    {
        // Arrange
        var channel = PrivateChannelAccessibleToCaller();

        _userRepository
            .Setup(x => x.GetByUsernameAsync("ghost"))
            .ReturnsAsync((User?)null);

        // Act
        var action = async () => await _service.AddMemberAsync(
            _userId,
            channel.Id,
            new AddChannelMemberRequest { Username = "ghost" });

        // Assert
        await action.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found.");

        VerifyNothingAdded();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenUserIsAlreadyMember()
    {
        // Arrange
        var channel = PrivateChannelAccessibleToCaller();
        var user = new User { Id = Guid.NewGuid(), Username = "ana" };

        _userRepository
            .Setup(x => x.GetByUsernameAsync("ana"))
            .ReturnsAsync(user);

        _channelRepository
            .Setup(x => x.IsMemberAsync(channel.Id, user.Id))
            .ReturnsAsync(true);

        // Act
        var action = async () => await _service.AddMemberAsync(
            _userId,
            channel.Id,
            new AddChannelMemberRequest { Username = "ana" });

        // Assert
        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("User is already a member of this channel.");

        VerifyNothingAdded();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenCallerAddsThemselves()
    {
        // Arrange: the caller is necessarily a member already.
        var channel = PrivateChannelAccessibleToCaller();

        _userRepository
            .Setup(x => x.GetByUsernameAsync("luiz"))
            .ReturnsAsync(new User { Id = _userId, Username = "luiz" });

        _channelRepository
            .Setup(x => x.IsMemberAsync(channel.Id, _userId))
            .ReturnsAsync(true);

        // Act
        var action = async () => await _service.AddMemberAsync(
            _userId,
            channel.Id,
            new AddChannelMemberRequest { Username = "luiz" });

        // Assert
        await action.Should()
            .ThrowAsync<InvalidOperationException>();

        VerifyNothingAdded();
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenUsernameIsEmpty()
    {
        // Act
        var action = async () => await _service.AddMemberAsync(
            _userId,
            Guid.NewGuid(),
            new AddChannelMemberRequest { Username = "   " });

        // Assert
        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Username is required.");

        VerifyNothingAdded();
    }

    private Channel PrivateChannelAccessibleToCaller()
    {
        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = "squad",
            IsPrivate = true
        };

        _channelRepository
            .Setup(x => x.GetAccessibleByIdAsync(channel.Id, _userId))
            .ReturnsAsync(channel);

        return channel;
    }

    private void VerifyNothingAdded()
    {
        _channelRepository.Verify(
            x => x.AddMemberAsync(It.IsAny<ChannelMember>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}