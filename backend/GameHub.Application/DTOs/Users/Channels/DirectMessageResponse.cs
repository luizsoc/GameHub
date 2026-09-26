namespace GameHub.Application.DTOs.Channels;

// A direct message as seen by one of its two participants: Id is the
// channel (history, sending and the SignalR group use it as usual), UserId
// and Username are the *other* participant.
public class DirectMessageResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
