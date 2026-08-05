using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Contracts;

public sealed record SubmitAnswerRequest(
    [Required, StringLength(10000, MinimumLength = 1)] string Answer);

public sealed record StudentSubmissionResponse(
    string Id,
    string AssignmentId,
    string AssignmentTitle,
    string SubjectId,
    string SubjectName,
    string Answer,
    SubmissionStatus Status,
    decimal? Marks,
    decimal MaximumMarks,
    string Feedback,
    DateTime Deadline,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    DateTime UpdatedAt,
    bool CanUpdate);
