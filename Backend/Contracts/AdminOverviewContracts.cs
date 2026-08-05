using Backend.Models;

namespace Backend.Contracts;

public sealed record AdminDashboardResponse(
    long Teachers,
    long Students,
    long Courses,
    long Subjects,
    long Assignments,
    long Submissions,
    long PendingReviews);

public sealed record AdminAssignmentResponse(
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
    AssignmentStatus Status,
    int SubmissionCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AdminSubmissionResponse(
    string Id,
    string AssignmentId,
    string AssignmentTitle,
    string StudentId,
    string StudentName,
    string StudentEmail,
    string Answer,
    SubmissionStatus Status,
    decimal? Marks,
    decimal MaximumMarks,
    string Feedback,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    DateTime UpdatedAt);
