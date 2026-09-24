using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace GameHub.Api.Hubs;

public class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue("sub");
    }
}