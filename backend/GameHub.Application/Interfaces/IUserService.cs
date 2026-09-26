using GameHub.Application.DTOs.Users;

namespace GameHub.Application.Interfaces;

public interface IUserService
{
    // Other users whose username contains the term (the caller is left out).
    Task<IEnumerable<UserSearchResponse>> SearchAsync(
        Guid userId,
        string? username);
}
