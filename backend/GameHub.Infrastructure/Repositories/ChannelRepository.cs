using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;
using GameHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Repositories;

public class ChannelRepository : IChannelRepository
{
    private readonly GameHubDbContext _context;

    public ChannelRepository(GameHubDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Channel channel)
    {
        await _context.Channels.AddAsync(channel);
    }

    public async Task<IEnumerable<Channel>> GetAllAsync()
    {
        return await _context.Channels
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<Channel?> GetByIdAsync(Guid id)
    {
        return await _context.Channels
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.Channels
            .AnyAsync(x => x.Name == name);
    }
}