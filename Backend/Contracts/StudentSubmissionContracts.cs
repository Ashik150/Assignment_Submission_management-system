using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Contracts;

public sealed class SubmitAnswerRequest
{
    [StringLength(10000)]
    public string? Answer { get; init; }

    public IFormFile? Pdf { get; init; }

    public bool RemovePdf { get; init; }
}

public sealed record StudentSubmissionResponse(
    string Id,
    string AssignmentId,
    string AssignmentTitle,
    string SubjectId,
    string SubjectName,
    string Answer,
    string? PdfFileName,
    long? PdfFileSize,
    SubmissionStatus Status,
    decimal? Marks,
    decimal MaximumMarks,
    string Feedback,
    DateTime Deadline,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    DateTime UpdatedAt,
    bool CanUpdate);
