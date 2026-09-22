namespace GameHub.Application.DTOs.Messages;

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;

    public Guid ChannelId { get; set; }
}