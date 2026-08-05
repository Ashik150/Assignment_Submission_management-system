using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backend.Models;

public enum AssignmentStatus
{
    Draft,
    Published
}

public sealed class Assignment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public required string CourseId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public required string SubjectId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public required string TeacherId { get; set; }

    public DateTime Deadline { get; set; }

    public decimal MaximumMarks { get; set; }

    [BsonRepresentation(BsonType.String)]
    public AssignmentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
