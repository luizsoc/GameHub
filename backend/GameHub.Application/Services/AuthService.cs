using GameHub.Application.DTOs.Users;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;

namespace GameHub.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtService jwtService)
{
    _userRepository = userRepository;
    _unitOfWork = unitOfWork;
    _passwordHasher = passwordHasher;
    _jwtService = jwtService;
}

    public async Task<UserResponse> RegisterAsync(
        RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required.");
        }

        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.GetByUsernameAsync(username) is not null)
        {
            throw new InvalidOperationException(
                "A user with this username already exists.");
        }

        if (await _userRepository.GetByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email
        };

        user.PasswordHash = _passwordHasher.Hash(request.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<string> LoginAsync(LoginRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        throw new ArgumentException("Email is required.");
    }

    if (string.IsNullOrWhiteSpace(request.Password))
    {
        throw new ArgumentException("Password is required.");
    }

    var email = request.Email.Trim().ToLowerInvariant();

    var user = await _userRepository.GetByEmailAsync(email);

    if (user is null)
    {
        throw new UnauthorizedAccessException(
            "Invalid email or password.");
    }

    var passwordIsValid = _passwordHasher.Verify(
        request.Password,
        user.PasswordHash);

    if (!passwordIsValid)
    {
        throw new UnauthorizedAccessException(
            "Invalid email or password.");
    }

    return _jwtService.GenerateToken(user);
}
}