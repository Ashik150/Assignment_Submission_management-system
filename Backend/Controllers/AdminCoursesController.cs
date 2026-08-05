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
[Route("api/admin/courses")]
public sealed class AdminCoursesController(MongoDbContext database) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseResponse>>> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = Builders<Course>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = System.Text.RegularExpressions.Regex.Escape(search.Trim());
            filter = Builders<Course>.Filter.Regex(course => course.Name, new BsonRegularExpression(escaped, "i")) |
                Builders<Course>.Filter.Regex(course => course.Code, new BsonRegularExpression(escaped, "i"));
        }

        var courses = await database.Courses.Find(filter)
            .SortBy(course => course.Name)
            .ToListAsync(cancellationToken);
        var subjects = await database.Subjects.Find(Builders<Subject>.Filter.Empty)
            .Project(subject => subject.CourseId)
            .ToListAsync(cancellationToken);
        var subjectCounts = subjects.GroupBy(courseId => courseId)
            .ToDictionary(group => group.Key, group => group.Count());

        return Ok(courses.Select(course =>
            ToResponse(course, subjectCounts.GetValueOrDefault(course.Id!, 0))));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CourseResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidId();
        }

        var course = await database.Courses.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (course is null)
        {
            return NotFoundProblem();
        }

        var subjectCount = await database.Subjects.CountDocumentsAsync(
            subject => subject.CourseId == id,
            cancellationToken: cancellationToken);

        return Ok(ToResponse(course, checked((int)subjectCount)));
    }

    [HttpPost]
    public async Task<ActionResult<CourseResponse>> Create(
        CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = new Course
        {
            Name = request.Name.Trim(),
            Code = NormalizeCode(request.Code),
            Description = request.Description.Trim(),
            IsActive = request.IsActive
        };

        try
        {
            await database.Courses.InsertOneAsync(course, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ConflictProblem("A course with this code already exists.");
        }

        var response = ToResponse(course, 0);
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CourseResponse>> Update(
        string id,
        UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidId();
        }

        var course = await database.Courses.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (course is null)
        {
            return NotFoundProblem();
        }

        course.Name = request.Name.Trim();
        course.Code = NormalizeCode(request.Code);
        course.Description = request.Description.Trim();
        course.IsActive = request.IsActive;
        course.UpdatedAt = DateTime.UtcNow;

        try
        {
            await database.Courses.ReplaceOneAsync(
                candidate => candidate.Id == id,
                course,
                cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ConflictProblem("A course with this code already exists.");
        }

        var subjectCount = await database.Subjects.CountDocumentsAsync(
            subject => subject.CourseId == id,
            cancellationToken: cancellationToken);
        return Ok(ToResponse(course, checked((int)subjectCount)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidId();
        }

        var exists = await database.Courses.Find(course => course.Id == id)
            .AnyAsync(cancellationToken);
        if (!exists)
        {
            return NotFoundProblem();
        }

        var isReferenced = await database.Subjects.Find(subject => subject.CourseId == id)
            .AnyAsync(cancellationToken) ||
            await database.Assignments.Find(assignment => assignment.CourseId == id)
                .AnyAsync(cancellationToken);
        if (isReferenced)
        {
            return ConflictProblem(
                "This course has subjects or assignments. Remove those records or deactivate the course instead.");
        }

        await database.Courses.DeleteOneAsync(course => course.Id == id, cancellationToken);
        return NoContent();
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static CourseResponse ToResponse(Course course, int subjectCount) => new(
        course.Id!,
        course.Name,
        course.Code,
        course.Description,
        course.IsActive,
        subjectCount,
        course.CreatedAt,
        course.UpdatedAt);

    private ObjectResult InvalidId() =>
        Problem(title: "Invalid course ID", detail: "The course ID is invalid.", statusCode: 400);

    private ObjectResult NotFoundProblem() =>
        Problem(title: "Course not found", detail: "The requested course does not exist.", statusCode: 404);

    private ObjectResult ConflictProblem(string detail) =>
        Problem(title: "Course conflict", detail: detail, statusCode: 409);
}
