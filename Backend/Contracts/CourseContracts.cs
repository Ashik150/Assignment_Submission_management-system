using System.ComponentModel.DataAnnotations;

namespace Backend.Contracts;

public sealed record CreateCourseRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    [property: Required, StringLength(30, MinimumLength = 2)] string Code,
    [property: StringLength(500)] string Description,
    bool IsActive = true);

public sealed record UpdateCourseRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    [property: Required, StringLength(30, MinimumLength = 2)] string Code,
    [property: StringLength(500)] string Description,
    bool IsActive);

public sealed record CourseResponse(
    string Id,
    string Name,
    string Code,
    string Description,
    bool IsActive,
    int SubjectCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
