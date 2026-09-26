using System.Linq.Expressions;
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

    // The single definition of "can this user access this channel?", used by
    // every channel read (REST and SignalR go through the services). It is
    // translated to SQL, so private channels are filtered in the database.
    private static Expression<Func<Channel, bool>> AccessibleBy(Guid userId)
    {
        return channel => !channel.IsPrivate ||
            channel.Members.Any(member => member.UserId == userId);
    }

    public async Task AddAsync(Channel channel)
    {
        await _context.Channels.AddAsync(channel);
    }

    public async Task<IEnumerable<Channel>> GetAccessibleAsync(Guid userId)
    {
        return await _context.Channels
            .AsNoTracking()
            .Where(AccessibleBy(userId))
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<Channel?> GetAccessibleByIdAsync(Guid id, Guid userId)
    {
        return await _context.Channels
            .AsNoTracking()
            .Where(AccessibleBy(userId))
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.Channels
            .AnyAsync(x => x.Name == name);
    }

    public async Task<bool> IsMemberAsync(Guid channelId, Guid userId)
    {
        return await _context.ChannelMembers
            .AnyAsync(x => x.ChannelId == channelId && x.UserId == userId);
    }

    public async Task AddMemberAsync(ChannelMember member)
    {
        await _context.ChannelMembers.AddAsync(member);
    }
}
