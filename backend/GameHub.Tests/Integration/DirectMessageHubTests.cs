using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using FluentAssertions;
using GameHub.Application.DTOs.Channels;
using GameHub.Application.DTOs.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Tests.Integration;

// Direct messages over SignalR: the DirectMessageCreated notification reaches
// the two participants only, and the ChatHub applies the private channel rule
// to the conversation.
public class DirectMessageHubTests : IClassFixture<GameHubApiFactory>
{
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SilenceWindow = TimeSpan.FromMilliseconds(500);

    private readonly GameHubApiFactory _factory;

    public DirectMessageHubTests(GameHubApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NewDirectMessage_ShouldNotifyBothParticipantsOnly()
    {
        var a = await _factory.CreateUserAsync("a");
        var b = await _factory.CreateUserAsync("b");
        var c = await _factory.CreateUserAsync("c");

        await using var hubA = await ConnectAsync(a);
        await using var hubB = await ConnectAsync(b);
        await using var hubC = await ConnectAsync(c);
        var createdForA = ListenForDirectMessages(hubA);
        var createdForB = ListenForDirectMessages(hubB);
        var createdForC = ListenForDirectMessages(hubC);

        var response = await _factory.CreateClient(a).OpenDirectMessageAsync(b.Id);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dm = (await response.Content.ReadFromJsonAsync<DirectMessageResponse>())!;

        // Each participant gets it from their own point of view.
        var forA = await ReceiveAsync(createdForA);
        forA.Id.Should().Be(dm.Id);
        forA.UserId.Should().Be(b.Id);
        forA.Username.Should().Be(b.Username);

        var forB = await ReceiveAsync(createdForB);
        forB.Id.Should().Be(dm.Id);
        forB.UserId.Should().Be(a.Id);
        forB.Username.Should().Be(a.Username);

        await Task.Delay(SilenceWindow);
        createdForC.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task ReopeningExistingDirectMessage_ShouldNotNotifyAgain()
    {
        var a = await _factory.CreateUserAsync("a");
        var b = await _factory.CreateUserAsync("b");
        var clientA = _factory.CreateClient(a);

        (await clientA.OpenDirectMessageAsync(b.Id)).StatusCode.Should().Be(HttpStatusCode.Created);

        await using var hubB = await ConnectAsync(b);
        var createdForB = ListenForDirectMessages(hubB);

        (await clientA.OpenDirectMessageAsync(b.Id)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _factory.CreateClient(b).OpenDirectMessageAsync(a.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(SilenceWindow);
        createdForB.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task Participants_ShouldJoinAndChatInRealTime()
    {
        var a = await _factory.CreateUserAsync("a");
        var b = await _factory.CreateUserAsync("b");
        var dm = await OpenAsync(a, b);

        await using var hubA = await ConnectAsync(a);
        await using var hubB = await ConnectAsync(b);
        var receivedByA = ListenForMessages(hubA);

        await hubA.InvokeAsync("JoinChannel", dm.Id);
        await hubB.InvokeAsync("JoinChannel", dm.Id);
        await SendAsync(hubB, dm.Id, "hi from b");

        var message = await ReceiveAsync(receivedByA);
        message.Content.Should().Be("hi from b");
        message.ChannelId.Should().Be(dm.Id);
    }

    [Fact]
    public async Task Outsider_ShouldNotJoinSendOrReceive()
    {
        var a = await _factory.CreateUserAsync("a");
        var b = await _factory.CreateUserAsync("b");
        var c = await _factory.CreateUserAsync("c");
        var dm = await OpenAsync(a, b);

        await using var hubA = await ConnectAsync(a);
        await using var hubC = await ConnectAsync(c);
        var receivedByA = ListenForMessages(hubA);
        var receivedByC = ListenForMessages(hubC);

        await hubA.InvokeAsync("JoinChannel", dm.Id);

        // C knows the conversation id and invokes the hub by hand.
        var join = () => hubC.InvokeAsync("JoinChannel", dm.Id);
        await join.Should().ThrowAsync<HubException>().WithMessage("*Channel not found*");

        var send = () => SendAsync(hubC, dm.Id, "intruder via hub");
        await send.Should().ThrowAsync<HubException>();

        (await _factory.QueryDbAsync(db => db.Messages.AnyAsync(x => x.Content == "intruder via hub")))
            .Should().BeFalse();

        // A's own message reaches A, never C.
        await SendAsync(hubA, dm.Id, "private");
        (await ReceiveAsync(receivedByA)).Content.Should().Be("private");

        await Task.Delay(SilenceWindow);
        receivedByC.TryRead(out _).Should().BeFalse();
        receivedByA.TryRead(out _).Should().BeFalse();
    }

    private async Task<DirectMessageResponse> OpenAsync(TestUser from, TestUser to)
    {
        var response = await _factory.CreateClient(from).OpenDirectMessageAsync(to.Id);

        return (await response.Content.ReadFromJsonAsync<DirectMessageResponse>())!;
    }

    private async Task<HubConnection> ConnectAsync(TestUser user)
    {
        var connection = _factory.CreateHubConnection(user);
        await connection.StartAsync();

        return connection;
    }

    private static ChannelReader<DirectMessageResponse> ListenForDirectMessages(HubConnection connection)
    {
        var received = Channel.CreateUnbounded<DirectMessageResponse>();

        connection.On<DirectMessageResponse>("DirectMessageCreated", dm =>
            received.Writer.TryWrite(dm));

        return received.Reader;
    }

    private static ChannelReader<MessageResponse> ListenForMessages(HubConnection connection)
    {
        var received = Channel.CreateUnbounded<MessageResponse>();

        connection.On<MessageResponse>("ReceiveMessage", message =>
            received.Writer.TryWrite(message));

        return received.Reader;
    }

    private static async Task<T> ReceiveAsync<T>(ChannelReader<T> received)
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
