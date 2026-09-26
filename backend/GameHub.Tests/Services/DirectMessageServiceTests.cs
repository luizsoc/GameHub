using FluentAssertions;
using GameHub.Application.DTOs.Channels;
using GameHub.Application.Interfaces;
using GameHub.Application.Services;
using GameHub.Domain.Entities;
using Moq;

namespace GameHub.Tests.Services;

public class DirectMessageServiceTests
{
    private readonly Mock<IChannelRepository> _channelRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly User _luiz = new() { Id = Guid.NewGuid(), Username = "luiz" };
    private readonly User _ana = new() { Id = Guid.NewGuid(), Username = "ana" };

    public DirectMessageServiceTests()
    {
        _userRepository.Setup(x => x.GetByIdAsync(_luiz.Id)).ReturnsAsync(_luiz);
        _userRepository.Setup(x => x.GetByIdAsync(_ana.Id)).ReturnsAsync(_ana);
    }

    private DirectMessageService CreateService()
    {
        return new DirectMessageService(
            _channelRepository.Object,
            _userRepository.Object,
            _unitOfWork.Object);
    }

    private string Key => Channel.DirectMessageKeyFor(_luiz.Id, _ana.Id);

    private Channel ExistingDirectMessage()
    {
        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = Channel.DirectMessageNamePrefix + Key,
            IsPrivate = true,
            DirectMessageKey = Key
        };

        channel.Members.Add(new ChannelMember { UserId = _luiz.Id, User = _luiz, ChannelId = channel.Id });
        channel.Members.Add(new ChannelMember { UserId = _ana.Id, User = _ana, ChannelId = channel.Id });

        return channel;
    }

    [Fact]
    public void DirectMessageKeyFor_ShouldNotDependOnOrder()
    {
        Channel.DirectMessageKeyFor(_luiz.Id, _ana.Id)
            .Should().Be(Channel.DirectMessageKeyFor(_ana.Id, _luiz.Id));

        Channel.DirectMessageKeyFor(_luiz.Id, _ana.Id).Length
            .Should().BeLessThanOrEqualTo(Channel.DirectMessageKeyMaxLength);
    }

    [Fact]
    public async Task OpenAsync_ShouldCreatePrivateChannelWithBothParticipants()
    {
        Channel? added = null;

        _channelRepository
            .Setup(x => x.AddAsync(It.IsAny<Channel>()))
            .Callback<Channel>(channel => added = channel);

        var result = await CreateService().OpenAsync(
            _luiz.Id,
            new OpenDirectMessageRequest { UserId = _ana.Id });

        result.Created.Should().BeTrue();

        added.Should().NotBeNull();
        added!.IsPrivate.Should().BeTrue();
        added.DirectMessageKey.Should().Be(Key);
        added.Name.Should().StartWith(Channel.DirectMessageNamePrefix);
        added.Members.Select(x => x.UserId).Should().BeEquivalentTo(new[] { _luiz.Id, _ana.Id });

        // Each side sees the other participant.
        result.ForCaller.Id.Should().Be(added.Id);
        result.ForCaller.UserId.Should().Be(_ana.Id);
        result.ForCaller.Username.Should().Be("ana");
        result.ForRecipient.Id.Should().Be(added.Id);
        result.ForRecipient.UserId.Should().Be(_luiz.Id);
        result.ForRecipient.Username.Should().Be("luiz");

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OpenAsync_ShouldReuseExistingConversation_FromEitherSide()
    {
        var existing = ExistingDirectMessage();

        _channelRepository
            .Setup(x => x.GetDirectMessageAsync(Key))
            .ReturnsAsync(existing);

        var fromLuiz = await CreateService().OpenAsync(
            _luiz.Id,
            new OpenDirectMessageRequest { UserId = _ana.Id });

        var fromAna = await CreateService().OpenAsync(
            _ana.Id,
            new OpenDirectMessageRequest { UserId = _luiz.Id });

        fromLuiz.Created.Should().BeFalse();
        fromAna.Created.Should().BeFalse();
        fromLuiz.ForCaller.Id.Should().Be(existing.Id);
        fromAna.ForCaller.Id.Should().Be(existing.Id);
        fromAna.ForCaller.Username.Should().Be("luiz");

        _channelRepository.Verify(x => x.AddAsync(It.IsAny<Channel>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OpenAsync_ShouldReturnConcurrentConversation_WhenInsertIsRejected()
    {
        // Nothing when checked; created by a concurrent request right before
        // our insert, which the unique index rejects.
        var concurrent = ExistingDirectMessage();

        _channelRepository
            .SetupSequence(x => x.GetDirectMessageAsync(Key))
            .ReturnsAsync((Channel?)null)
            .ReturnsAsync(concurrent);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("unique index violation"));

        var result = await CreateService().OpenAsync(
            _luiz.Id,
            new OpenDirectMessageRequest { UserId = _ana.Id });

        result.Created.Should().BeFalse();
        result.ForCaller.Id.Should().Be(concurrent.Id);
    }

    [Fact]
    public async Task OpenAsync_ShouldRethrow_WhenInsertFailsForAnotherReason()
    {
        _channelRepository
            .Setup(x => x.GetDirectMessageAsync(Key))
            .ReturnsAsync((Channel?)null);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database is down"));

        var act = () => CreateService().OpenAsync(
            _luiz.Id,
            new OpenDirectMessageRequest { UserId = _ana.Id });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("database is down");
    }

    [Fact]
    public async Task OpenAsync_ShouldThrow_WhenOpeningWithYourself()
    {
        var act = () => CreateService().OpenAsync(
            _luiz.Id,
            new OpenDirectMessageRequest { UserId = _luiz.Id });

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("You cannot start a direct message with yourself.");

        _channelRepository.Verify(x => x.AddAsync(It.IsAny<Channel>()), Times.Never);
    }

    [Fact]
    public async Task OpenAsync_ShouldThrow_WhenUserIdIsEmpty()
    {
        var act = () => CreateService().OpenAsync(
            _luiz.Id,
            new OpenDirectMessageRequest { UserId = Guid.Empty });

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("UserId is required.");
    }

    [Fact]
    public async Task OpenAsync_ShouldThrow_WhenOtherUserDoesNotExist()
    {
        var act = () => CreateService().OpenAsync(
            _luiz.Id,
            new OpenDirectMessageRequest { UserId = Guid.NewGuid() });

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found.");

        _channelRepository.Verify(x => x.AddAsync(It.IsAny<Channel>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_ShouldShowTheOtherParticipant()
    {
        _channelRepository
            .Setup(x => x.GetDirectMessagesAsync(_ana.Id))
            .ReturnsAsync(new List<Channel> { ExistingDirectMessage() });

        var result = (await CreateService().GetAllAsync(_ana.Id)).ToList();

        result.Should().ContainSingle();
        result[0].UserId.Should().Be(_luiz.Id);
        result[0].Username.Should().Be("luiz");
    }
}
