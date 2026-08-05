using Backend.Models;

namespace Backend.Contracts;

public sealed record StudentDashboardResponse(
    string CourseId,
    string CourseName,
    long AvailableAssignments,
    long DueThisWeek,
    long SubmittedAssignments,
    long AwaitingSubmission);

public sealed record StudentAssignmentResponse(
    string Id,
    string Title,
    string Description,
    string CourseId,
    string CourseName,
    string SubjectId,
    string SubjectName,
    string TeacherId,
    string TeacherName,
    DateTime Deadline,
    decimal MaximumMarks,
    DateTime CreatedAt,
    string? SubmissionId,
    SubmissionStatus? SubmissionStatus,
    decimal? Marks,
    string? Feedback,
    bool CanSubmit,
    bool CanUpdateSubmission);
