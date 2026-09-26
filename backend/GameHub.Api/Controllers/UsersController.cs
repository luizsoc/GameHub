using System.Security.Claims;
using GameHub.Application.DTOs.Users;
using GameHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // GET /api/users/search?username=luiz: up to 10 other users (id and
    // username only) to start a direct message with.
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserSearchResponse>>> Search(
        [FromQuery] string? username)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        try
        {
            var users = await _userService.SearchAsync(userId, username);

            return Ok(users);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
