using FluentAssertions;
using GameHub.Application.DTOs.Users;
using GameHub.Application.Interfaces;
using GameHub.Application.Services;
using GameHub.Domain.Entities;
using Moq;

namespace GameHub.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtServiceMock = new Mock<IJwtService>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(x => x.Hash("Password123!"))
            .Returns("hashed-password");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.Is<User>(user =>
                user.Username == "testuser" &&
                user.Email == "test@example.com" &&
                user.PasswordHash == "hashed-password")),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenUsernameAlreadyExists()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "existinguser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("existinguser"))
            .ReturnsAsync(new User());

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("A user with this username already exists.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("existing@example.com"))
            .ReturnsAsync(new User());

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("A user with this email already exists.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenUsernameIsEmpty()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "",
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Username is required.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenEmailIsEmpty()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "",
            Password = "Password123!"
        };

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Email is required.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenPasswordIsEmpty()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Password is required.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify("Password123!", "hashed-password"))
            .Returns(true);

        _jwtServiceMock
            .Setup(x => x.GenerateToken(user))
            .Returns("fake-jwt-token");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().Be("fake-jwt-token");

        _jwtServiceMock.Verify(
            x => x.GenerateToken(user),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenEmailDoesNotExist()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "notfound@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("notfound@example.com"))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordIsInvalid()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword!"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify("WrongPassword!", "hashed-password"))
            .Returns(false);

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenEmailIsEmpty()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "",
            Password = "Password123!"
        };

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Email is required.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordIsEmpty()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Password is required.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenUsernameIsTooLong()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = new string('a', User.UsernameMaxLength + 1),
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage($"Username must be at most {User.UsernameMaxLength} characters.");

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>()),
            Times.Never);
    }
}