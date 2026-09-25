namespace GameHub.Domain.Entities;

public class Channel
{
    // Column limits (GameHubDbContext), also enforced by ChannelService.
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public ICollection<ChannelMember> Members { get; set; } = new List<ChannelMember>();
}