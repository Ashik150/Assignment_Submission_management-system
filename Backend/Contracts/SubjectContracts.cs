using System.ComponentModel.DataAnnotations;

namespace Backend.Contracts;

public sealed record CreateSubjectRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    [property: Required, StringLength(30, MinimumLength = 2)] string Code,
    [property: Required] string CourseId,
    string? TeacherId,
    bool IsActive = true);

public sealed record UpdateSubjectRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    [property: Required, StringLength(30, MinimumLength = 2)] string Code,
    [property: Required] string CourseId,
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
