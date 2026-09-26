using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);

    // Case-insensitive "contains" on the username, ordered by username.
    Task<IEnumerable<User>> SearchByUsernameAsync(
        string term,
        Guid excludedUserId,
        int limit);
}