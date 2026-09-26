namespace GameHub.Domain.Entities;

public class Channel
{
    // Column limits (GameHubDbContext), also enforced by ChannelService.
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    // "<guid>:<guid>" in the 32-digit format.
    public const int DirectMessageKeyMaxLength = 65;

    // Direct messages are named "dm:<key>"; regular channels cannot use the
    // prefix, so their names never collide with a direct message's.
    public const string DirectMessageNamePrefix = "dm:";

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Public channels are open to every authenticated user; private ones only
    // to the users in Members.
    public bool IsPrivate { get; set; }

    // Set only on direct messages (a private channel with exactly two
    // members): the pair of participants, see DirectMessageKeyFor. Unique in
    // the database, so a pair never has two conversations. Null on regular
    // channels.
    public string? DirectMessageKey { get; set; }

    public bool IsDirectMessage => DirectMessageKey is not null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public ICollection<ChannelMember> Members { get; set; } = new List<ChannelMember>();

    // Same key whichever of the two starts the conversation (A → B = B → A).
    public static string DirectMessageKeyFor(Guid userId, Guid otherUserId)
    {
        var (first, second) = userId.CompareTo(otherUserId) <= 0
            ? (userId, otherUserId)
            : (otherUserId, userId);

        return $"{first:N}:{second:N}";
    }
}
