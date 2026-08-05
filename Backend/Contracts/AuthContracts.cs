using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Contracts;

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record AuthenticatedUser(
    string Id,
    string FullName,
    string Email,
    UserRole Role);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    AuthenticatedUser User);
