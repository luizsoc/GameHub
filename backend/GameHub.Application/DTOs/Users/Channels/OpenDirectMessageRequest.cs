namespace GameHub.Application.DTOs.Channels;

public class OpenDirectMessageRequest
{
    // The other participant.
    public Guid UserId { get; set; }
}
