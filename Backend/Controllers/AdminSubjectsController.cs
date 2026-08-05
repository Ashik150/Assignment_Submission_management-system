using Backend.Contracts;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Backend.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/subjects")]
public sealed class AdminSubjectsController(MongoDbContext database) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubjectResponse>>> GetAll(
        [FromQuery] string? courseId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = Builders<Subject>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(courseId))
        {
            if (!ObjectId.TryParse(courseId, out _))
            {
                return InvalidRequest("The course ID is invalid.");
            }

            filter &= Builders<Subject>.Filter.Eq(subject => subject.CourseId, courseId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = System.Text.RegularExpressions.Regex.Escape(search.Trim());
            filter &= Builders<Subject>.Filter.Regex(
                    subject => subject.Name,
                    new BsonRegularExpression(escaped, "i")) |
                Builders<Subject>.Filter.Regex(
                    subject => subject.Code,
                    new BsonRegularExpression(escaped, "i"));
        }

        var subjects = await database.Subjects.Find(filter)
            .SortBy(subject => subject.Name)
            .ToListAsync(cancellationToken);
        var courses = await database.Courses.Find(Builders<Course>.Filter.Empty)
            .ToListAsync(cancellationToken);
        var teachers = await database.Users.Find(user => user.Role == UserRole.Teacher)
            .ToListAsync(cancellationToken);
        var courseNames = courses.ToDictionary(course => course.Id!, course => course.Name);
        var teacherNames = teachers.ToDictionary(teacher => teacher.Id!, teacher => teacher.FullName);

        return Ok(subjects.Select(subject => ToResponse(
            subject,
            courseNames.GetValueOrDefault(subject.CourseId, "Unknown course"),
            subject.TeacherId is null ? null : teacherNames.GetValueOrDefault(subject.TeacherId))));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The subject ID is invalid.");
        }

        var subject = await database.Subjects.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (subject is null)
        {
            return NotFoundProblem();
        }

        var course = await database.Courses.Find(candidate => candidate.Id == subject.CourseId)
            .FirstOrDefaultAsync(cancellationToken);
        var teacher = subject.TeacherId is null
            ? null
            : await database.Users.Find(candidate => candidate.Id == subject.TeacherId)
                .FirstOrDefaultAsync(cancellationToken);

        return Ok(ToResponse(subject, course?.Name ?? "Unknown course", teacher?.FullName));
    }

    [HttpPost]
    public async Task<ActionResult<SubjectResponse>> Create(
        CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var relationResult = await ResolveRelations(
            request.CourseId,
            request.TeacherId,
            cancellationToken);
        if (relationResult.Error is not null)
        {
            return relationResult.Error;
        }

        var subject = new Subject
        {
            Name = request.Name.Trim(),
            Code = NormalizeCode(request.Code),
            CourseId = request.CourseId,
            TeacherId = NormalizeOptionalId(request.TeacherId),
            IsActive = request.IsActive
        };

        try
        {
            await database.Subjects.InsertOneAsync(subject, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ConflictProblem("This course already has a subject with the same code.");
        }

        var response = ToResponse(subject, relationResult.Course!.Name, relationResult.Teacher?.FullName);
        return CreatedAtAction(nameof(GetById), new { id = subject.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SubjectResponse>> Update(
        string id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The subject ID is invalid.");
        }

        var subject = await database.Subjects.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (subject is null)
        {
            return NotFoundProblem();
        }

        var relationResult = await ResolveRelations(
            request.CourseId,
            request.TeacherId,
            cancellationToken);
        if (relationResult.Error is not null)
        {
            return relationResult.Error;
        }

        subject.Name = request.Name.Trim();
        subject.Code = NormalizeCode(request.Code);
        subject.CourseId = request.CourseId;
        subject.TeacherId = NormalizeOptionalId(request.TeacherId);
        subject.IsActive = request.IsActive;
        subject.UpdatedAt = DateTime.UtcNow;

        try
        {
            await database.Subjects.ReplaceOneAsync(
                candidate => candidate.Id == id,
                subject,
                cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ConflictProblem("This course already has a subject with the same code.");
        }

        return Ok(ToResponse(subject, relationResult.Course!.Name, relationResult.Teacher?.FullName));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The subject ID is invalid.");
        }

        var exists = await database.Subjects.Find(subject => subject.Id == id)
            .AnyAsync(cancellationToken);
        if (!exists)
        {
            return NotFoundProblem();
        }

        var hasAssignments = await database.Assignments.Find(assignment => assignment.SubjectId == id)
            .AnyAsync(cancellationToken);
        if (hasAssignments)
        {
            return ConflictProblem(
                "This subject has assignments. Remove those records or deactivate the subject instead.");
        }

        await database.Subjects.DeleteOneAsync(subject => subject.Id == id, cancellationToken);
        return NoContent();
    }

    private async Task<(Course? Course, User? Teacher, ObjectResult? Error)> ResolveRelations(
        string courseId,
        string? teacherId,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(courseId, out _))
        {
            return (null, null, InvalidRequest("The course ID is invalid."));
        }

        var course = await database.Courses.Find(candidate => candidate.Id == courseId)
            .FirstOrDefaultAsync(cancellationToken);
        if (course is null)
        {
            return (null, null, InvalidRequest("The selected course does not exist."));
        }

        var normalizedTeacherId = NormalizeOptionalId(teacherId);
        if (normalizedTeacherId is null)
        {
            return (course, null, null);
        }

        if (!ObjectId.TryParse(normalizedTeacherId, out _))
        {
            return (null, null, InvalidRequest("The teacher ID is invalid."));
        }

        var teacher = await database.Users.Find(user =>
                user.Id == normalizedTeacherId && user.Role == UserRole.Teacher)
            .FirstOrDefaultAsync(cancellationToken);
        return teacher is null
            ? (null, null, InvalidRequest("The selected teacher does not exist or is not a teacher."))
            : (course, teacher, null);
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static string? NormalizeOptionalId(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : id.Trim();

    private static SubjectResponse ToResponse(Subject subject, string courseName, string? teacherName) => new(
        subject.Id!,
        subject.Name,
        subject.Code,
        subject.CourseId,
        courseName,
        subject.TeacherId,
        teacherName,
        subject.IsActive,
        subject.CreatedAt,
        subject.UpdatedAt);

    private ObjectResult InvalidRequest(string detail) =>
        Problem(title: "Invalid subject request", detail: detail, statusCode: 400);

    private ObjectResult NotFoundProblem() =>
        Problem(title: "Subject not found", detail: "The requested subject does not exist.", statusCode: 404);

    private ObjectResult ConflictProblem(string detail) =>
        Problem(title: "Subject conflict", detail: detail, statusCode: 409);
}
