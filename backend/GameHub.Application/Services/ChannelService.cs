using GameHub.Application.DTOs.Channels;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;

namespace GameHub.Application.Services;

public class ChannelService : IChannelService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChannelService(
        IChannelRepository channelRepository,
        IUnitOfWork unitOfWork)
    {
        _channelRepository = channelRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChannelResponse> CreateAsync(
        CreateChannelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Channel name is required.");
        }

        var name = request.Name.Trim();

        if (await _channelRepository.ExistsByNameAsync(name))
        {
            throw new InvalidOperationException(
                "A channel with this name already exists.");
        }

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = request.Description?.Trim()
        };

        await _channelRepository.AddAsync(channel);
        await _unitOfWork.SaveChangesAsync();

        return new ChannelResponse
        {
            Id = channel.Id,
            Name = channel.Name,
            Description = channel.Description,
            CreatedAt = channel.CreatedAt
        };
    }

    public async Task<IEnumerable<ChannelResponse>> GetAllAsync()
    {
        var channels = await _channelRepository.GetAllAsync();

        return channels.Select(channel => new ChannelResponse
        {
            Id = channel.Id,
            Name = channel.Name,
            Description = channel.Description,
            CreatedAt = channel.CreatedAt
        });
    }

    public async Task<ChannelResponse?> GetByIdAsync(Guid id)
    {
        var channel = await _channelRepository.GetByIdAsync(id);

        if (channel is null)
        {
            return null;
        }

        return new ChannelResponse
        {
            Id = channel.Id,
            Name = channel.Name,
            Description = channel.Description,
            CreatedAt = channel.CreatedAt
        };
    }
}