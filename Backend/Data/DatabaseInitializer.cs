using Backend.Configuration;
using Backend.Models;
using Backend.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Backend.Data;

public sealed class DatabaseInitializer(
    MongoDbContext database,
    PasswordHasher passwordHasher,
    IOptions<SeedSettings> seedOptions,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await CreateIndexesAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateIndexesAsync(CancellationToken cancellationToken)
    {
        await database.Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(user => user.Email),
                new CreateIndexOptions { Unique = true, Name = "uq_users_email" }),
            cancellationToken: cancellationToken);

        await database.Courses.Indexes.CreateOneAsync(
            new CreateIndexModel<Course>(
                Builders<Course>.IndexKeys.Ascending(course => course.Code),
                new CreateIndexOptions { Unique = true, Name = "uq_courses_code" }),
            cancellationToken: cancellationToken);

        await database.Subjects.Indexes.CreateOneAsync(
            new CreateIndexModel<Subject>(
                Builders<Subject>.IndexKeys
                    .Ascending(subject => subject.CourseId)
                    .Ascending(subject => subject.Code),
                new CreateIndexOptions { Unique = true, Name = "uq_subjects_course_code" }),
            cancellationToken: cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var settings = seedOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.AdminPassword))
        {
            logger.LogWarning("Admin seed skipped because Seed__AdminPassword is not configured.");
            return;
        }

        var email = settings.AdminEmail.Trim().ToLowerInvariant();
        var exists = await database.Users.Find(user => user.Email == email)
            .AnyAsync(cancellationToken);

        if (exists)
        {
            return;
        }

        await database.Users.InsertOneAsync(new User
        {
            FullName = settings.AdminName.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(settings.AdminPassword),
            Role = UserRole.Admin
        }, cancellationToken: cancellationToken);

        logger.LogInformation("Created the initial administrator account for {Email}.", email);
    }
}
