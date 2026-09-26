using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GameHub.Application.DTOs.Channels;
using GameHub.Application.DTOs.Messages;
using GameHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Tests.Integration;

// Direct messages over REST, against the real API: user search, open or
// create (also under concurrent calls), and the private channel rules applied
// to a conversation between two users.
public class DirectMessageApiTests : IClassFixture<GameHubApiFactory>
{
    private readonly GameHubApiFactory _factory;

    public DirectMessageApiTests(GameHubApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(TestUser A, TestUser B, HttpClient ClientA, HttpClient ClientB)> TwoUsersAsync()
    {
        var a = await _factory.CreateUserAsync("a");
        var b = await _factory.CreateUserAsync("b");

        return (a, b, _factory.CreateClient(a), _factory.CreateClient(b));
    }

    private static async Task<DirectMessageResponse> ReadAsync(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<DirectMessageResponse>())!;
    }

    // ---- Search

    [Fact]
    public async Task Search_ShouldFindUserByPartOfUsername_CaseInsensitive()
    {
        var (a, b, clientA, _) = await TwoUsersAsync();

        var response = await clientA.SearchUsersAsync(b.Username[2..10].ToUpperInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(b.Username);
        body.Should().Contain(b.Id.ToString());

        // Only what is needed to pick the user.
        body.Should().NotContain("email", because: "the search exposes id and username only");
        body.Should().NotContainEquivalentOf("password");
        body.Should().NotContain("token");
    }

    [Fact]
    public async Task Search_ShouldNotReturnTheCaller()
    {
        var (a, _, clientA, _) = await TwoUsersAsync();

        var body = await (await clientA.SearchUsersAsync(a.Username)).Content.ReadAsStringAsync();

        body.Should().NotContain(a.Id.ToString());
    }

    [Fact]
    public async Task Search_ShouldReturnEmptyList_WhenNobodyMatches()
    {
        var (_, _, clientA, _) = await TwoUsersAsync();

        var response = await clientA.SearchUsersAsync("nobody-has-this-name");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("[]");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_ShouldRejectEmptyTerm(string term)
    {
        var (_, _, clientA, _) = await TwoUsersAsync();

        (await clientA.SearchUsersAsync(term)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await clientA.GetAsync("/api/users/search")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldRejectTooLongTerm()
    {
        var (_, _, clientA, _) = await TwoUsersAsync();

        var response = await clientA.SearchUsersAsync(new string('a', User.UsernameMaxLength + 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldRequireAuthentication()
    {
        var anonymous = _factory.CreateClient(user: null);

        (await anonymous.SearchUsersAsync("a")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Open or create

    [Fact]
    public async Task Open_ShouldCreateOnceAndReuseFromBothSides()
    {
        var (a, b, clientA, clientB) = await TwoUsersAsync();

        var first = await clientA.OpenDirectMessageAsync(b.Id);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadAsync(first);
        created.UserId.Should().Be(b.Id);
        created.Username.Should().Be(b.Username);

        var again = await clientA.OpenDirectMessageAsync(b.Id);
        again.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(again)).Id.Should().Be(created.Id);

        // B → A finds the same conversation, showing A.
        var reverse = await clientB.OpenDirectMessageAsync(a.Id);
        reverse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fromB = await ReadAsync(reverse);
        fromB.Id.Should().Be(created.Id);
        fromB.UserId.Should().Be(a.Id);
        fromB.Username.Should().Be(a.Username);

        await AssertSingleDirectMessageAsync(a, b, created.Id);
    }

    [Fact]
    public async Task Open_ShouldListTheConversationForBothOnly()
    {
        var (a, b, clientA, clientB) = await TwoUsersAsync();
        var c = await _factory.CreateUserAsync("c");

        var dm = await ReadAsync(await clientA.OpenDirectMessageAsync(b.Id));

        (await clientA.GetDirectMessagesAsync()).Should().ContainSingle(x => x.Id == dm.Id && x.UserId == b.Id);
        (await clientB.GetDirectMessagesAsync()).Should().ContainSingle(x => x.Id == dm.Id && x.UserId == a.Id);
        (await _factory.CreateClient(c).GetDirectMessagesAsync()).Should().NotContain(x => x.Id == dm.Id);

        // Not part of the regular channel list.
        (await clientA.GetChannelsAsync()).Should().NotContain(x => x.Id == dm.Id);
    }

    [Fact]
    public async Task Open_ShouldRejectInvalidRequests()
    {
        var (a, _, clientA, _) = await TwoUsersAsync();

        (await clientA.OpenDirectMessageAsync(a.Id)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await clientA.OpenDirectMessageAsync(Guid.Empty)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await clientA.OpenDirectMessageAsync(Guid.NewGuid())).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var anonymous = _factory.CreateClient(user: null);
        (await anonymous.OpenDirectMessageAsync(a.Id)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/direct-messages")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Open_ConcurrentCallsFromBothSides_ShouldCreateASingleConversation()
    {
        var (a, b, clientA, clientB) = await TwoUsersAsync();

        // 10 × A → B and 10 × B → A at the same time.
        var calls = Enumerable.Range(0, 20)
            .Select(i => i % 2 == 0
                ? clientA.OpenDirectMessageAsync(b.Id)
                : clientB.OpenDirectMessageAsync(a.Id))
            .ToList();

        var responses = await Task.WhenAll(calls);

        responses.Select(x => x.StatusCode).Should()
            .OnlyContain(status => status == HttpStatusCode.OK || status == HttpStatusCode.Created);

        // Exactly one call created it; every call got the same conversation.
        responses.Count(x => x.StatusCode == HttpStatusCode.Created).Should().Be(1);

        var ids = new HashSet<Guid>();
        foreach (var response in responses)
        {
            ids.Add((await ReadAsync(response)).Id);
        }

        ids.Should().ContainSingle();

        await AssertSingleDirectMessageAsync(a, b, ids.Single());
    }

    // ---- Private channel rules on a direct message

    [Fact]
    public async Task Participants_ShouldReadAndSend()
    {
        var (_, b, clientA, clientB) = await TwoUsersAsync();
        var dm = await ReadAsync(await clientA.OpenDirectMessageAsync(b.Id));

        (await clientA.PostMessageAsync(dm.Id, "hey b")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await clientB.PostMessageAsync(dm.Id, "hey a")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await clientB.GetChannelAsync(dm.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await clientB.GetMessagesAsync(dm.Id);
        history.StatusCode.Should().Be(HttpStatusCode.OK);
        (await history.Content.ReadFromJsonAsync<List<MessageResponse>>())!
            .Select(x => x.Content).Should().Equal("hey b", "hey a");
    }

    [Fact]
    public async Task Outsider_ShouldBeBlockedEverywhere()
    {
        var (_, b, clientA, _) = await TwoUsersAsync();
        var c = await _factory.CreateUserAsync("c");
        var clientC = _factory.CreateClient(c);

        var dm = await ReadAsync(await clientA.OpenDirectMessageAsync(b.Id));
        (await clientA.PostMessageAsync(dm.Id, "just between us")).StatusCode.Should().Be(HttpStatusCode.OK);

        // C knows the conversation id and calls the endpoints directly.
        (await clientC.GetChannelAsync(dm.Id)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var history = await clientC.GetMessagesAsync(dm.Id);
        history.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await history.Content.ReadAsStringAsync()).Should().NotContain("just between us");

        (await clientC.PostMessageAsync(dm.Id, "intruder")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await _factory.QueryDbAsync(db => db.Messages.AnyAsync(x => x.Content == "intruder")))
            .Should().BeFalse();
    }

    [Fact]
    public async Task AddMember_ShouldNotAllowAThirdPerson()
    {
        var (a, b, clientA, _) = await TwoUsersAsync();
        var c = await _factory.CreateUserAsync("c");

        var dm = await ReadAsync(await clientA.OpenDirectMessageAsync(b.Id));

        var response = await clientA.AddMemberAsync(dm.Id, c.Username);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertSingleDirectMessageAsync(a, b, dm.Id);
    }

    [Fact]
    public async Task CreateChannel_ShouldRejectTheReservedDirectMessagePrefix()
    {
        var (_, _, clientA, _) = await TwoUsersAsync();

        var response = await clientA.PostAsJsonAsync("/api/channels", new CreateChannelRequest
        {
            Name = $"dm:{Guid.NewGuid():N}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // One private direct message for the pair, with exactly A and B.
    private async Task AssertSingleDirectMessageAsync(TestUser a, TestUser b, Guid expectedId)
    {
        var key = Channel.DirectMessageKeyFor(a.Id, b.Id);

        var channels = await _factory.QueryDbAsync(db => db.Channels
            .Where(x => x.DirectMessageKey == key)
            .Select(x => new { x.Id, x.IsPrivate, Members = x.Members.Select(m => m.UserId).ToList() })
            .ToListAsync());

        channels.Should().ContainSingle();
        channels[0].Id.Should().Be(expectedId);
        channels[0].IsPrivate.Should().BeTrue();
        channels[0].Members.Should().BeEquivalentTo(new[] { a.Id, b.Id });
    }
}
