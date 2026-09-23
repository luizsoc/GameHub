using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}