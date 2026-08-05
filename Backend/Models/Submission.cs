using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backend.Models;

public enum SubmissionStatus
{
    Submitted,
    Late,
    Reviewed,
    Returned
}

public sealed class Submission
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public required string AssignmentId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public required string StudentId { get; set; }

    public string Answer { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? PdfFileId { get; set; }

    [BsonIgnoreIfNull]
    public string? PdfFileName { get; set; }

    [BsonIgnoreIfNull]
    public long? PdfFileSize { get; set; }

    [BsonRepresentation(BsonType.String)]
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    [BsonIgnoreIfNull]
    public decimal? Marks { get; set; }

    public string Feedback { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [BsonIgnoreIfNull]
    public DateTime? ReviewedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
