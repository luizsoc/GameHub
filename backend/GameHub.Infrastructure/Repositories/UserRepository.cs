using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;
using GameHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly GameHubDbContext _context;

    public UserRepository(GameHubDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<IEnumerable<User>> SearchByUsernameAsync(
        string term,
        Guid excludedUserId,
        int limit)
    {
        var lowerTerm = term.ToLower();

        return await _context.Users
            .AsNoTracking()
            .Where(x => x.Id != excludedUserId &&
                x.Username.ToLower().Contains(lowerTerm))
            .OrderBy(x => x.Username)
            .Take(limit)
            .ToListAsync();
    }
}