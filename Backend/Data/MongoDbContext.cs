using Backend.Models;
using MongoDB.Driver;

namespace Backend.Data;

public sealed class MongoDbContext(IMongoDatabase database)
{
    public IMongoCollection<User> Users { get; } = database.GetCollection<User>("users");

    public IMongoCollection<Course> Courses { get; } = database.GetCollection<Course>("courses");

    public IMongoCollection<Subject> Subjects { get; } = database.GetCollection<Subject>("subjects");

    public IMongoCollection<Assignment> Assignments { get; } = database.GetCollection<Assignment>("assignments");

    public IMongoCollection<Submission> Submissions { get; } = database.GetCollection<Submission>("submissions");
}
