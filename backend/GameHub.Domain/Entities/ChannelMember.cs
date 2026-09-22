namespace GameHub.Domain.Entities;

public class ChannelMember
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid ChannelId { get; set; }

    public Channel Channel { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}