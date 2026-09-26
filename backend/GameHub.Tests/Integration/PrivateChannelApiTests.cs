using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GameHub.Application.DTOs.Channels;
using GameHub.Application.DTOs.Messages;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Tests.Integration;

// Authorization of private channels over REST, against the real API. The key
// scenario: user B knows the id of user A's private channel and calls the
// endpoints directly, bypassing the UI.
public class PrivateChannelApiTests : IClassFixture<GameHubApiFactory>
{
    private readonly GameHubApiFactory _factory;

    public PrivateChannelApiTests(GameHubApiFactory factory)
    {
        _factory = factory;
    }

    // User A, user B, a public channel and a private channel created by A
    // (with one message from A in it).
    private async Task<Scenario> ArrangeAsync()
    {
        var userA = await _factory.CreateUserAsync("a");
        var userB = await _factory.CreateUserAsync("b");
        var clientA = _factory.CreateClient(userA);
        var clientB = _factory.CreateClient(userB);

        var publicChannel = await clientA.CreateChannelAsync(isPrivate: false);
        var privateChannel = await clientA.CreateChannelAsync(
            isPrivate: true,
            description: "secret description");

        var send = await clientA.PostMessageAsync(privateChannel.Id, "secret message");
        send.StatusCode.Should().Be(HttpStatusCode.OK);

        return new Scenario(userA, userB, clientA, clientB, publicChannel, privateChannel);
    }

    [Fact]
    public async Task CreatePrivateChannel_ShouldMakeCreatorItsOnlyMember()
    {
        var s = await ArrangeAsync();

        s.PrivateChannel.IsPrivate.Should().BeTrue();

        var members = await _factory.QueryDbAsync(db => db.ChannelMembers
            .Where(x => x.ChannelId == s.PrivateChannel.Id)
            .Select(x => x.UserId)
            .ToListAsync());

        members.Should().Equal(s.UserA.Id);
    }

    [Fact]
    public async Task CreatePublicChannel_ShouldKeepCurrentBehavior()
    {
        var s = await ArrangeAsync();

        s.PublicChannel.IsPrivate.Should().BeFalse();

        var memberCount = await _factory.QueryDbAsync(db => db.ChannelMembers
            .CountAsync(x => x.ChannelId == s.PublicChannel.Id));

        memberCount.Should().Be(0);

        // Any authenticated user reads and writes in a public channel.
        (await s.ClientB.GetChannelAsync(s.PublicChannel.Id)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await s.ClientB.PostMessageAsync(s.PublicChannel.Id, "hi")).StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await s.ClientA.GetMessagesAsync(s.PublicChannel.Id);
        history.StatusCode.Should().Be(HttpStatusCode.OK);
        (await history.Content.ReadFromJsonAsync<List<MessageResponse>>())!
            .Should().ContainSingle(x => x.Content == "hi");
    }

    [Fact]
    public async Task Member_ShouldListAccessReadAndSend()
    {
        var s = await ArrangeAsync();

        var channels = await s.ClientA.GetChannelsAsync();
        channels.Should().Contain(x => x.Id == s.PrivateChannel.Id && x.IsPrivate);
        channels.Should().Contain(x => x.Id == s.PublicChannel.Id && !x.IsPrivate);

        var channel = await s.ClientA.GetChannelAsync(s.PrivateChannel.Id);
        channel.StatusCode.Should().Be(HttpStatusCode.OK);

        (await s.ClientA.PostMessageAsync(s.PrivateChannel.Id, "second")).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var history = await s.ClientA.GetMessagesAsync(s.PrivateChannel.Id);
        history.StatusCode.Should().Be(HttpStatusCode.OK);
        (await history.Content.ReadFromJsonAsync<List<MessageResponse>>())!
            .Select(x => x.Content).Should().Equal("secret message", "second");
    }

    [Fact]
    public async Task NonMember_ShouldNotReceivePrivateChannelInList()
    {
        var s = await ArrangeAsync();

        var channels = await s.ClientB.GetChannelsAsync();

        channels.Should().Contain(x => x.Id == s.PublicChannel.Id);
        channels.Should().NotContain(x => x.Id == s.PrivateChannel.Id);
    }

    [Fact]
    public async Task NonMember_ShouldNotAccessPrivateChannelById()
    {
        var s = await ArrangeAsync();

        var response = await s.ClientB.GetChannelAsync(s.PrivateChannel.Id);

        // Same answer as for a channel that does not exist.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await s.ClientB.GetChannelAsync(Guid.NewGuid())).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(s.PrivateChannel.Name);
        body.Should().NotContain("secret description");
    }

    [Fact]
    public async Task NonMember_ShouldNotReadPrivateHistory()
    {
        var s = await ArrangeAsync();

        var response = await s.ClientB.GetMessagesAsync(s.PrivateChannel.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("secret message");
    }

    [Fact]
    public async Task NonMember_ShouldNotSendToPrivateChannel()
    {
        var s = await ArrangeAsync();

        var response = await s.ClientB.PostMessageAsync(s.PrivateChannel.Id, "intruder");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var stored = await _factory.QueryDbAsync(db => db.Messages
            .AnyAsync(x => x.Content == "intruder"));

        stored.Should().BeFalse();
    }

    [Fact]
    public async Task NonMember_ShouldNotAddMembers()
    {
        var s = await ArrangeAsync();

        // B tries to add themselves to A's private channel.
        var response = await s.ClientB.AddMemberAsync(s.PrivateChannel.Id, s.UserB.Username);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await IsMemberAsync(s.PrivateChannel.Id, s.UserB.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Unauthenticated_ShouldBeRejectedEverywhere()
    {
        var s = await ArrangeAsync();
        var anonymous = _factory.CreateClient(user: null);

        (await anonymous.GetAsync("/api/channels")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetChannelAsync(s.PrivateChannel.Id)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetMessagesAsync(s.PrivateChannel.Id)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.PostMessageAsync(s.PrivateChannel.Id, "anon")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.AddMemberAsync(s.PrivateChannel.Id, s.UserB.Username)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/channels", new CreateChannelRequest { Name = "anon", IsPrivate = true }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddedMember_ShouldGainFullAccess()
    {
        var s = await ArrangeAsync();

        var add = await s.ClientA.AddMemberAsync(s.PrivateChannel.Id, s.UserB.Username);

        add.StatusCode.Should().Be(HttpStatusCode.OK);
        var member = (await add.Content.ReadFromJsonAsync<ChannelMemberResponse>())!;
        member.UserId.Should().Be(s.UserB.Id);
        member.Username.Should().Be(s.UserB.Username);
        member.ChannelId.Should().Be(s.PrivateChannel.Id);

        (await s.ClientB.GetChannelsAsync()).Should().Contain(x => x.Id == s.PrivateChannel.Id);
        (await s.ClientB.GetChannelAsync(s.PrivateChannel.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await s.ClientB.GetMessagesAsync(s.PrivateChannel.Id);
        history.StatusCode.Should().Be(HttpStatusCode.OK);
        (await history.Content.ReadFromJsonAsync<List<MessageResponse>>())!
            .Should().Contain(x => x.Content == "secret message");

        (await s.ClientB.PostMessageAsync(s.PrivateChannel.Id, "hello from b")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddMember_Twice_ShouldConflictWithoutDuplicates()
    {
        var s = await ArrangeAsync();

        (await s.ClientA.AddMemberAsync(s.PrivateChannel.Id, s.UserB.Username)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var again = await s.ClientA.AddMemberAsync(s.PrivateChannel.Id, s.UserB.Username);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // A member (the creator) adding themselves is the same case.
        var self = await s.ClientA.AddMemberAsync(s.PrivateChannel.Id, s.UserA.Username);
        self.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var rows = await _factory.QueryDbAsync(db => db.ChannelMembers
            .Where(x => x.ChannelId == s.PrivateChannel.Id)
            .GroupBy(x => x.UserId)
            .Select(g => g.Count())
            .ToListAsync());

        rows.Should().Equal(1, 1);
    }

    [Fact]
    public async Task AddMember_UnknownUsername_ShouldReturnNotFound()
    {
        var s = await ArrangeAsync();

        var response = await s.ClientA.AddMemberAsync(s.PrivateChannel.Id, "nobody-with-this-name");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("User not found.");

        var memberCount = await _factory.QueryDbAsync(db => db.ChannelMembers
            .CountAsync(x => x.ChannelId == s.PrivateChannel.Id));

        memberCount.Should().Be(1);
    }

    [Fact]
    public async Task AddMember_ToPublicChannel_ShouldBeRejected()
    {
        var s = await ArrangeAsync();

        var response = await s.ClientA.AddMemberAsync(s.PublicChannel.Id, s.UserB.Username);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Member_ShouldOnlyAccessTheirOwnPrivateChannels()
    {
        // A is a member of Private A only; Private B belongs to user C.
        var userA = await _factory.CreateUserAsync("a");
        var userC = await _factory.CreateUserAsync("c");
        var clientA = _factory.CreateClient(userA);
        var clientC = _factory.CreateClient(userC);

        var privateA = await clientA.CreateChannelAsync(isPrivate: true);
        var privateB = await clientC.CreateChannelAsync(isPrivate: true);
        (await clientC.PostMessageAsync(privateB.Id, "only for c")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await clientA.GetChannelAsync(privateA.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var channels = await clientA.GetChannelsAsync();
        channels.Should().Contain(x => x.Id == privateA.Id);
        channels.Should().NotContain(x => x.Id == privateB.Id);

        (await clientA.GetChannelAsync(privateB.Id)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var history = await clientA.GetMessagesAsync(privateB.Id);
        history.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await history.Content.ReadAsStringAsync()).Should().NotContain("only for c");

        (await clientA.PostMessageAsync(privateB.Id, "wrong channel")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        (await clientA.AddMemberAsync(privateB.Id, userA.Username)).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        (await IsMemberAsync(privateB.Id, userA.Id)).Should().BeFalse();
    }

    private Task<bool> IsMemberAsync(Guid channelId, Guid userId)
    {
        return _factory.QueryDbAsync(db => db.ChannelMembers
            .AnyAsync(x => x.ChannelId == channelId && x.UserId == userId));
    }

    private record Scenario(
        TestUser UserA,
        TestUser UserB,
        HttpClient ClientA,
        HttpClient ClientB,
        ChannelResponse PublicChannel,
        ChannelResponse PrivateChannel);
}
