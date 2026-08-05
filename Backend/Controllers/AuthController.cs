using Backend.Contracts;
using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    MongoDbContext database,
    PasswordHasher passwordHasher,
    TokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await database.Users.Find(candidate => candidate.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = "The email or password is incorrect, or the account is inactive.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var (token, expiresAt) = tokenService.Create(user);
        return Ok(new LoginResponse(
            token,
            expiresAt,
            new AuthenticatedUser(user.Id!, user.FullName, user.Email, user.Role)));
    }
}
