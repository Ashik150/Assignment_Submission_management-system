using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Contracts;

public sealed record CreateUserRequest(
    [property: Required, StringLength(100, MinimumLength = 2)] string FullName,
    [property: Required, EmailAddress, StringLength(200)] string Email,
    [property: Required, MinLength(8), MaxLength(100)] string Password,
    [property: Required] UserRole Role,
    bool IsActive = true);

public sealed record UpdateUserRequest(
    [property: Required, StringLength(100, MinimumLength = 2)] string FullName,
    [property: Required, EmailAddress, StringLength(200)] string Email,
    [property: MinLength(8), MaxLength(100)] string? Password,
    [property: Required] UserRole Role,
    bool IsActive);

public sealed record UserResponse(
    string Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
