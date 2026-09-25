using FluentAssertions;
using GameHub.Domain.Entities;
using GameHub.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace GameHub.Tests.Security;

public class JwtServiceTests
{
    [Fact]
    public void GenerateToken_ShouldGenerateValidJwt()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "GameHub-development-secret-key-change-this"
            })
            .Build();

        var jwtService = new JwtService(configuration);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token = jwtService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(
            claim => claim.Type == JwtRegisteredClaimNames.Sub &&
                     claim.Value == user.Id.ToString());

        jwt.Claims.Should().Contain(
            claim => claim.Type == JwtRegisteredClaimNames.UniqueName &&
                     claim.Value == user.Username);

        jwt.Claims.Should().Contain(
            claim => claim.Type == JwtRegisteredClaimNames.Email &&
                     claim.Value == user.Email);

        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_ShouldThrow_WhenJwtKeyIsNotConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        var jwtService = new JwtService(configuration);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var act = () => jwtService.GenerateToken(user);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("JWT key is not configured.");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeIssuerAndAudience_WhenConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "GameHub-development-secret-key-change-this",
                ["Jwt:Issuer"] = "gamehub-test",
                ["Jwt:Audience"] = "gamehub-test-clients"
            })
            .Build();

        var jwtService = new JwtService(configuration);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(
            jwtService.GenerateToken(user));

        // Assert
        jwt.Issuer.Should().Be("gamehub-test");
        jwt.Audiences.Should().ContainSingle()
            .Which.Should().Be("gamehub-test-clients");
    }
}