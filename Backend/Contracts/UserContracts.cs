using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Contracts;

public sealed record CreateUserRequest(
    [Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(200)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required] UserRole Role,
    string? CourseId,
    bool IsActive = true);

public sealed record UpdateUserRequest(
    [Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(200)] string Email,
    [MinLength(8), MaxLength(100)] string? Password,
    [Required] UserRole Role,
    string? CourseId,
    bool IsActive);

public sealed record UserResponse(
    string Id,
    string FullName,
    string Email,
    UserRole Role,
    string? CourseId,
    string? CourseName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
