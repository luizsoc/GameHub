using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;
using GameHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly GameHubDbContext _context;

    public MessageRepository(GameHubDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Message message)
    {
        await _context.Messages.AddAsync(message);
    }

    public async Task<IEnumerable<Message>> GetByChannelAsync(
        Guid channelId)
    {
        return await _context.Messages
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.ChannelId == channelId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}