using CommerceFlow.Application.Auth.Commands.LoginUser;
using CommerceFlow.Application.Auth.Commands.RegisterUser;
using CommerceFlow.Application.Auth.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter for the auth slice. Register and login are the two
// doorways in and carry [AllowAnonymous]; every other endpoint in the API
// is behind the fallback policy and needs the token these issue.
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    // Creates an account. There is no user-by-id endpoint (the account is
    // read back through /me), so 201 carries the id directly instead of a
    // Location header pointing nowhere useful.
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = await sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id = userId });
    }

    // Credentials in, access token + identity out (200, not 201: nothing was
    // created - the token is a signed statement about an existing account).
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    // Proof that the claims survived the round trip: who the verified token
    // says the caller is, straight from the database.
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(result);
    }
}
