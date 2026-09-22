namespace GameHub.Application.DTOs.Messages;

public class MessageResponse
{
    public Guid Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public Guid ChannelId { get; set; }

    public DateTime CreatedAt { get; set; }
}