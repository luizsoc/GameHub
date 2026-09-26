namespace GameHub.Application.DTOs.Channels;

// Result of opening a direct message: the conversation from each
// participant's point of view (ForCaller.UserId is the recipient,
// ForRecipient.UserId the caller), and whether this call created it (only
// then both participants are notified).
public class OpenDirectMessageResult
{
    public DirectMessageResponse ForCaller { get; set; } = new();

    public DirectMessageResponse ForRecipient { get; set; } = new();

    public bool Created { get; set; }
}
