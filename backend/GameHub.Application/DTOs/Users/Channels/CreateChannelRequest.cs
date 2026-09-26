namespace GameHub.Application.DTOs.Channels;

public class CreateChannelRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Optional: omitted means a public channel, as before.
    public bool IsPrivate { get; set; }
}