using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Contracts;

public sealed record ReviewSubmissionRequest(
    [Range(typeof(decimal), "0", "10000")] decimal? Marks,
    [StringLength(2000)] string Feedback,
    SubmissionStatus Status);

public sealed record TeacherSubmissionResponse(
    string Id,
    string AssignmentId,
    string AssignmentTitle,
    string StudentId,
    string StudentName,
    string StudentEmail,
    string Answer,
    string? PdfFileName,
    long? PdfFileSize,
    SubmissionStatus Status,
    decimal? Marks,
    decimal MaximumMarks,
    string Feedback,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    DateTime UpdatedAt);
