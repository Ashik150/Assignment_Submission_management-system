using System.ComponentModel.DataAnnotations;

namespace Backend.Contracts;

public sealed record CreateSubjectRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [Required, StringLength(30, MinimumLength = 2)] string Code,
    [Required] string CourseId,
    string? TeacherId,
    bool IsActive = true);

public sealed record UpdateSubjectRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [Required, StringLength(30, MinimumLength = 2)] string Code,
    [Required] string CourseId,
    string? TeacherId,
    bool IsActive);

public sealed record SubjectResponse(
    string Id,
    string Name,
    string Code,
    string CourseId,
    string CourseName,
    string? TeacherId,
    string? TeacherName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
