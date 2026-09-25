namespace GameHub.Domain.Entities;

public class Message
{
    // Column limit (GameHubDbContext), also enforced by MessageService.
    public const int ContentMaxLength = 2000;

    public Guid Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid ChannelId { get; set; }

    public Channel Channel { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}