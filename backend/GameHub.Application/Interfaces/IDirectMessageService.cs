using GameHub.Application.DTOs.Channels;

namespace GameHub.Application.Interfaces;

public interface IDirectMessageService
{
    // Returns the existing conversation with the other user, or creates it.
    Task<OpenDirectMessageResult> OpenAsync(
        Guid userId,
        OpenDirectMessageRequest request);

    Task<IEnumerable<DirectMessageResponse>> GetAllAsync(Guid userId);
}
