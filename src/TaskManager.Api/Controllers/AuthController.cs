using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskManager.Application.Auth;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService service) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<UserView>> Register(RegisterRequest request, CancellationToken ct)
    {
        var user = await service.RegisterAsync(request.Name, request.Email, request.Password, ct);
        return Created("/api/auth/me", user);
    }
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<Session>> Login(LoginRequest request, CancellationToken ct)
        => Ok(await service.LoginAsync(request.Email, request.Password, ct));
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserView>> Me(CancellationToken ct)
        => Ok(await service.GetUserAsync(Guid.Parse(User.FindFirst("sub")!.Value), ct));
}

public sealed record RegisterRequest([Required] string Name, [Required] string Email, [Required] string Password);
public sealed record LoginRequest([Required] string Email, [Required] string Password);
