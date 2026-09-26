using GameHub.Application.DTOs.Users;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;

namespace GameHub.Application.Services;

public class UserService : IUserService
{
    // Enough to pick someone; the list is not meant to browse every user.
    public const int SearchLimit = 10;

    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<UserSearchResponse>> SearchAsync(
        Guid userId,
        string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.");
        }

        var term = username.Trim();

        if (term.Length > User.UsernameMaxLength)
        {
            throw new ArgumentException(
                $"Username must be at most {User.UsernameMaxLength} characters.");
        }

        var users = await _userRepository
            .SearchByUsernameAsync(term, userId, SearchLimit);

        return users.Select(user => new UserSearchResponse
        {
            Id = user.Id,
            Username = user.Username
        });
    }
}
