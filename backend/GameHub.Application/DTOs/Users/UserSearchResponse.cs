namespace GameHub.Application.DTOs.Users;

// Only what is needed to pick someone (no email or anything else).
public class UserSearchResponse
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;
}
