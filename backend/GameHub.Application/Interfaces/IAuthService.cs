using GameHub.Application.DTOs.Users;

namespace GameHub.Application.Interfaces;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterUserRequest request);

    Task<string> LoginAsync(LoginRequest request);
}