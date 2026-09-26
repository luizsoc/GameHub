namespace GameHub.Application.DTOs.Channels;

public class ChannelResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPrivate { get; set; }

    public DateTime CreatedAt { get; set; }
}