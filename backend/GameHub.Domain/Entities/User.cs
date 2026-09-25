namespace GameHub.Domain.Entities;

public class User
{
    // Column limits (GameHubDbContext), also enforced by AuthService.
    public const int UsernameMaxLength = 50;
    public const int EmailMaxLength = 255;

    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public ICollection<ChannelMember> ChannelMemberships { get; set; } = new List<ChannelMember>();
}