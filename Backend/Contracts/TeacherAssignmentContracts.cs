using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Contracts;

public sealed record CreateAssignmentRequest(
    [Required, StringLength(180, MinimumLength = 3)] string Title,
    [StringLength(5000)] string Description,
    [Required] string CourseId,
    [Required] string SubjectId,
    DateTime Deadline,
    [Range(typeof(decimal), "0.01", "10000")] decimal MaximumMarks,
    AssignmentStatus Status);

public sealed record UpdateAssignmentRequest(
    [Required, StringLength(180, MinimumLength = 3)] string Title,
    [StringLength(5000)] string Description,
    [Required] string CourseId,
    [Required] string SubjectId,
    DateTime Deadline,
    [Range(typeof(decimal), "0.01", "10000")] decimal MaximumMarks,
    AssignmentStatus Status);

public sealed record TeacherAssignmentResponse(
    string Id,
    string Title,
    string Description,
    string CourseId,
    string CourseName,
    string SubjectId,
    string SubjectName,
    DateTime Deadline,
    decimal MaximumMarks,
    AssignmentStatus Status,
    int SubmissionCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TeacherSubjectResponse(
    string Id,
    string Name,
    string Code,
    string CourseId,
    string CourseName,
    bool IsActive,
    bool IsCourseActive);

public sealed record TeacherDashboardResponse(
    long AssignedSubjects,
    long Assignments,
    long PublishedAssignments,
    long Submissions,
    long PendingReviews);
