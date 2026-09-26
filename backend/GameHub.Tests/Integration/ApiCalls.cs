using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GameHub.Application.DTOs.Channels;
using GameHub.Application.DTOs.Messages;

namespace GameHub.Tests.Integration;

// Thin wrappers over the REST endpoints used by the integration tests.
public static class ApiCalls
{
    public static async Task<ChannelResponse> CreateChannelAsync(
        this HttpClient client,
        bool isPrivate,
        string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/channels", new CreateChannelRequest
        {
            Name = $"channel-{Guid.NewGuid():N}",
            Description = description,
            IsPrivate = isPrivate
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<ChannelResponse>())!;
    }

    public static async Task<List<ChannelResponse>> GetChannelsAsync(this HttpClient client)
    {
        var response = await client.GetAsync("/api/channels");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<List<ChannelResponse>>())!;
    }

    public static Task<HttpResponseMessage> GetChannelAsync(this HttpClient client, Guid channelId)
    {
        return client.GetAsync($"/api/channels/{channelId}");
    }

    public static Task<HttpResponseMessage> GetMessagesAsync(this HttpClient client, Guid channelId)
    {
        return client.GetAsync($"/api/messages/channel/{channelId}");
    }

    public static Task<HttpResponseMessage> PostMessageAsync(
        this HttpClient client,
        Guid channelId,
        string content)
    {
        return client.PostAsJsonAsync("/api/messages", new SendMessageRequest
        {
            ChannelId = channelId,
            Content = content
        });
    }

    public static Task<HttpResponseMessage> AddMemberAsync(
        this HttpClient client,
        Guid channelId,
        string username)
    {
        return client.PostAsJsonAsync($"/api/channels/{channelId}/members", new AddChannelMemberRequest
        {
            Username = username
        });
    }
}
