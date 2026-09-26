using System.Net;
using System.Threading.Channels;
using FluentAssertions;
using GameHub.Application.DTOs.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Tests.Integration;

// Authorization of private channels over SignalR (ChatHub), with real hub
// connections: a user who knows a private channel's id and invokes the hub
// methods by hand must not get in.
public class PrivateChannelHubTests : IClassFixture<GameHubApiFactory>
{
    // Time a message that *should* arrive is given; a message that must not
    // arrive is checked after the expected one was received plus this delay.
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SilenceWindow = TimeSpan.FromMilliseconds(500);

    private readonly GameHubApiFactory _factory;

    public PrivateChannelHubTests(GameHubApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_ShouldNotConnect()
    {
        await using var connection = _factory.CreateHubConnection(user: null);

        var connect = () => connection.StartAsync();

        (await connect.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Member_ShouldJoinSendAndReceive()
    {
        var userA = await _factory.CreateUserAsync("a");
        var privateChannel = await _factory.CreateClient(userA).CreateChannelAsync(isPrivate: true);

        await using var hubA = await ConnectAsync(userA);
        var receivedByA = Listen(hubA);

        await hubA.InvokeAsync("JoinChannel", privateChannel.Id);
        await SendAsync(hubA, privateChannel.Id, "hello team");

        var message = await ReceiveAsync(receivedByA);
        message.Content.Should().Be("hello team");
        message.ChannelId.Should().Be(privateChannel.Id);
        message.UserId.Should().Be(userA.Id);
    }

    [Fact]
    public async Task NonMember_ShouldNotJoinPrivateChannel()
    {
        var userA = await _factory.CreateUserAsync("a");
        var userB = await _factory.CreateUserAsync("b");
        var privateChannel = await _factory.CreateClient(userA).CreateChannelAsync(isPrivate: true);

        await using var hubA = await ConnectAsync(userA);
        await using var hubB = await ConnectAsync(userB);
        var receivedByA = Listen(hubA);
        var receivedByB = Listen(hubB);

        await hubA.InvokeAsync("JoinChannel", privateChannel.Id);

        // B calls JoinChannel with the known id, bypassing the UI.
        var join = () => hubB.InvokeAsync("JoinChannel", privateChannel.Id);

        await join.Should().ThrowAsync<HubException>()
            .WithMessage("*Channel not found*");

        // B was not added to the group: A's message never reaches B.
        await SendAsync(hubA, privateChannel.Id, "members only");
        (await ReceiveAsync(receivedByA)).Content.Should().Be("members only");

        await Task.Delay(SilenceWindow);
        receivedByB.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task NonMember_ShouldNotSendToPrivateChannel()
    {
        var userA = await _factory.CreateUserAsync("a");
        var userB = await _factory.CreateUserAsync("b");
        var privateChannel = await _factory.CreateClient(userA).CreateChannelAsync(isPrivate: true);

        await using var hubA = await ConnectAsync(userA);
        await using var hubB = await ConnectAsync(userB);
        var receivedByA = Listen(hubA);

        await hubA.InvokeAsync("JoinChannel", privateChannel.Id);

        // B invokes SendMessage directly with the known id.
        var send = () => SendAsync(hubB, privateChannel.Id, "intruder via hub");

        await send.Should().ThrowAsync<HubException>();

        // Nothing stored and nothing broadcast to the members.
        var stored = await _factory.QueryDbAsync(db => db.Messages
            .AnyAsync(x => x.Content == "intruder via hub"));

        stored.Should().BeFalse();

        await Task.Delay(SilenceWindow);
        receivedByA.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task AddedMember_ShouldJoinAndSend()
    {
        var userA = await _factory.CreateUserAsync("a");
        var userB = await _factory.CreateUserAsync("b");
        var clientA = _factory.CreateClient(userA);
        var privateChannel = await clientA.CreateChannelAsync(isPrivate: true);

        (await clientA.AddMemberAsync(privateChannel.Id, userB.Username)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        await using var hubA = await ConnectAsync(userA);
        await using var hubB = await ConnectAsync(userB);
        var receivedByA = Listen(hubA);
        var receivedByB = Listen(hubB);

        await hubA.InvokeAsync("JoinChannel", privateChannel.Id);
        await hubB.InvokeAsync("JoinChannel", privateChannel.Id);

        await SendAsync(hubB, privateChannel.Id, "thanks for the invite");

        (await ReceiveAsync(receivedByA)).Content.Should().Be("thanks for the invite");
        (await ReceiveAsync(receivedByB)).Content.Should().Be("thanks for the invite");
    }

    [Fact]
    public async Task Member_ShouldNotJoinOrSendToAnotherPrivateChannel()
    {
        // A is a member of Private A only; Private B belongs to user C.
        var userA = await _factory.CreateUserAsync("a");
        var userC = await _factory.CreateUserAsync("c");
        var privateA = await _factory.CreateClient(userA).CreateChannelAsync(isPrivate: true);
        var privateB = await _factory.CreateClient(userC).CreateChannelAsync(isPrivate: true);

        await using var hubA = await ConnectAsync(userA);
        await using var hubC = await ConnectAsync(userC);
        var receivedByC = Listen(hubC);

        await hubA.InvokeAsync("JoinChannel", privateA.Id);
        await hubC.InvokeAsync("JoinChannel", privateB.Id);

        var join = () => hubA.InvokeAsync("JoinChannel", privateB.Id);
        await join.Should().ThrowAsync<HubException>();

        var send = () => SendAsync(hubA, privateB.Id, "wrong channel via hub");
        await send.Should().ThrowAsync<HubException>();

        var stored = await _factory.QueryDbAsync(db => db.Messages
            .AnyAsync(x => x.Content == "wrong channel via hub"));

        stored.Should().BeFalse();

        await Task.Delay(SilenceWindow);
        receivedByC.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task PublicChannel_ShouldStayOpenToEveryUser()
    {
        var userA = await _factory.CreateUserAsync("a");
        var userB = await _factory.CreateUserAsync("b");
        var publicChannel = await _factory.CreateClient(userA).CreateChannelAsync(isPrivate: false);

        await using var hubA = await ConnectAsync(userA);
        await using var hubB = await ConnectAsync(userB);
        var receivedByA = Listen(hubA);

        await hubA.InvokeAsync("JoinChannel", publicChannel.Id);
        await hubB.InvokeAsync("JoinChannel", publicChannel.Id);

        await SendAsync(hubB, publicChannel.Id, "open to all");

        (await ReceiveAsync(receivedByA)).Content.Should().Be("open to all");
    }

    private async Task<HubConnection> ConnectAsync(TestUser user)
    {
        var connection = _factory.CreateHubConnection(user);
        await connection.StartAsync();

        return connection;
    }

    private static ChannelReader<MessageResponse> Listen(HubConnection connection)
    {
        var received = Channel.CreateUnbounded<MessageResponse>();

        connection.On<MessageResponse>("ReceiveMessage", message =>
            received.Writer.TryWrite(message));

        return received.Reader;
    }

    private static async Task<MessageResponse> ReceiveAsync(ChannelReader<MessageResponse> received)
    {
        using var timeout = new CancellationTokenSource(ReceiveTimeout);

        return await received.ReadAsync(timeout.Token);
    }

    private static Task SendAsync(HubConnection connection, Guid channelId, string content)
    {
        return connection.InvokeAsync("SendMessage", new SendMessageRequest
        {
            ChannelId = channelId,
            Content = content
        });
    }
}
