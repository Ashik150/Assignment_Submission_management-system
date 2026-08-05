using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Configuration;
using Backend.Models;
using Backend.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Tests.Services;

public sealed class TokenServiceTests
{
    private const string Secret = "unit-test-secret-that-is-at-least-sixty-four-characters-long-123456789";

    [Fact]
    public void Create_IssuesValidTokenWithIdentityAndRoleClaims()
    {
        var settings = Settings();
        var service = new TokenService(Options.Create(settings));
        var user = User();

        var (encodedToken, expiresAt) = service.Create(user);
        var principal = new JwtSecurityTokenHandler().ValidateToken(
            encodedToken,
            ValidationParameters(settings),
            out var validatedToken);

        Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal(user.Id, principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(user.FullName, principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal(user.Email, principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(nameof(UserRole.Student), principal.FindFirstValue(ClaimTypes.Role));
        Assert.InRange(expiresAt, DateTime.UtcNow.AddHours(7.9), DateTime.UtcNow.AddHours(8.1));
    }

    [Fact]
    public void Create_UsesUniqueTokenIdentifiers()
    {
        var service = new TokenService(Options.Create(Settings()));

        var first = new JwtSecurityTokenHandler().ReadJwtToken(service.Create(User()).Token);
        var second = new JwtSecurityTokenHandler().ReadJwtToken(service.Create(User()).Token);

        Assert.NotEqual(
            first.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value,
            second.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value);
    }

    private static JwtSettings Settings() => new()
    {
        Issuer = "OnnoRokom.Tests",
        Audience = "OnnoRokom.Tests.Client",
        Secret = Secret
    };

    private static TokenValidationParameters ValidationParameters(JwtSettings settings) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = settings.Issuer,
        ValidAudience = settings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
        ClockSkew = TimeSpan.Zero
    };

    private static User User() => new()
    {
        Id = "507f1f77bcf86cd799439011",
        FullName = "Test Student",
        Email = "student@example.com",
        PasswordHash = "unused",
        Role = UserRole.Student,
        CourseId = "507f1f77bcf86cd799439012",
        IsActive = true
    };
}
