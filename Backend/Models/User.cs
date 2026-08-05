using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backend.Models;

public sealed class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public required string FullName { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? CourseId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
