namespace GameHub.Application.DTOs.Channels;

public class CreateChannelRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}