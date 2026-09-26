namespace GameHub.Application.DTOs.Channels;

public class ChannelMemberResponse
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public Guid ChannelId { get; set; }

    public DateTime JoinedAt { get; set; }
}
