using FluentAssertions;
using GameHub.Application.Interfaces;
using GameHub.Application.Services;
using GameHub.Domain.Entities;
using Moq;

namespace GameHub.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Guid _userId = Guid.NewGuid();

    private UserService CreateService()
    {
        return new UserService(_userRepository.Object);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnIdAndUsernameOfOtherUsers()
    {
        var ana = new User
        {
            Id = Guid.NewGuid(),
            Username = "ana",
            Email = "ana@example.test",
            PasswordHash = "hash"
        };

        _userRepository
            .Setup(x => x.SearchByUsernameAsync("an", _userId, UserService.SearchLimit))
            .ReturnsAsync(new List<User> { ana });

        var result = (await CreateService().SearchAsync(_userId, "  an  ")).ToList();

        // The trimmed term, the caller excluded, and only id and username back.
        result.Should().ContainSingle();
        result[0].Id.Should().Be(ana.Id);
        result[0].Username.Should().Be("ana");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenNobodyMatches()
    {
        _userRepository
            .Setup(x => x.SearchByUsernameAsync("ghost", _userId, UserService.SearchLimit))
            .ReturnsAsync(new List<User>());

        var result = await CreateService().SearchAsync(_userId, "ghost");

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_ShouldThrow_WhenUsernameIsEmpty(string? username)
    {
        var act = () => CreateService().SearchAsync(_userId, username);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Username is required.");

        _userRepository.Verify(
            x => x.SearchByUsernameAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_ShouldThrow_WhenUsernameIsTooLong()
    {
        var act = () => CreateService().SearchAsync(
            _userId,
            new string('a', User.UsernameMaxLength + 1));

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage($"Username must be at most {User.UsernameMaxLength} characters.");
    }
}
