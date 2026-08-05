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
[Route("api/admin")]
public sealed class AdminOverviewController(MongoDbContext database) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var teachersTask = database.Users.CountDocumentsAsync(
            user => user.Role == UserRole.Teacher,
            cancellationToken: cancellationToken);
        var studentsTask = database.Users.CountDocumentsAsync(
            user => user.Role == UserRole.Student,
            cancellationToken: cancellationToken);
        var coursesTask = database.Courses.CountDocumentsAsync(
            Builders<Course>.Filter.Empty,
            cancellationToken: cancellationToken);
        var subjectsTask = database.Subjects.CountDocumentsAsync(
            Builders<Subject>.Filter.Empty,
            cancellationToken: cancellationToken);
        var assignmentsTask = database.Assignments.CountDocumentsAsync(
            Builders<Assignment>.Filter.Empty,
            cancellationToken: cancellationToken);
        var submissionsTask = database.Submissions.CountDocumentsAsync(
            Builders<Submission>.Filter.Empty,
            cancellationToken: cancellationToken);
        var pendingReviewsTask = database.Submissions.CountDocumentsAsync(
            Builders<Submission>.Filter.In(
                submission => submission.Status,
                new[] { SubmissionStatus.Submitted, SubmissionStatus.Late }),
            cancellationToken: cancellationToken);

        await Task.WhenAll(
            teachersTask,
            studentsTask,
            coursesTask,
            subjectsTask,
            assignmentsTask,
            submissionsTask,
            pendingReviewsTask);

        return Ok(new AdminDashboardResponse(
            await teachersTask,
            await studentsTask,
            await coursesTask,
            await subjectsTask,
            await assignmentsTask,
            await submissionsTask,
            await pendingReviewsTask));
    }

    [HttpGet("assignments")]
    public async Task<ActionResult<IReadOnlyList<AdminAssignmentResponse>>> GetAssignments(
        [FromQuery] string? courseId,
        [FromQuery] string? subjectId,
        [FromQuery] AssignmentStatus? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = Builders<Assignment>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(courseId))
        {
            if (!ObjectId.TryParse(courseId, out _))
            {
                return InvalidFilter("The course ID is invalid.");
            }

            filter &= Builders<Assignment>.Filter.Eq(assignment => assignment.CourseId, courseId);
        }

        if (!string.IsNullOrWhiteSpace(subjectId))
        {
            if (!ObjectId.TryParse(subjectId, out _))
            {
                return InvalidFilter("The subject ID is invalid.");
            }

            filter &= Builders<Assignment>.Filter.Eq(assignment => assignment.SubjectId, subjectId);
        }

        if (status is not null)
        {
            filter &= Builders<Assignment>.Filter.Eq(assignment => assignment.Status, status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            filter &= Builders<Assignment>.Filter.Regex(
                assignment => assignment.Title,
                new BsonRegularExpression(
                    System.Text.RegularExpressions.Regex.Escape(search.Trim()),
                    "i"));
        }

        var assignments = await database.Assignments.Find(filter)
            .SortByDescending(assignment => assignment.CreatedAt)
            .ToListAsync(cancellationToken);
        var courses = await database.Courses.Find(Builders<Course>.Filter.Empty)
            .ToListAsync(cancellationToken);
        var subjects = await database.Subjects.Find(Builders<Subject>.Filter.Empty)
            .ToListAsync(cancellationToken);
        var teachers = await database.Users.Find(user => user.Role == UserRole.Teacher)
            .ToListAsync(cancellationToken);
        var submissions = await database.Submissions.Find(Builders<Submission>.Filter.Empty)
            .Project(submission => submission.AssignmentId)
            .ToListAsync(cancellationToken);

        var courseNames = courses.ToDictionary(course => course.Id!, course => course.Name);
        var subjectNames = subjects.ToDictionary(subject => subject.Id!, subject => subject.Name);
        var teacherNames = teachers.ToDictionary(teacher => teacher.Id!, teacher => teacher.FullName);
        var submissionCounts = submissions.GroupBy(assignmentId => assignmentId)
            .ToDictionary(group => group.Key, group => group.Count());

        return Ok(assignments.Select(assignment => new AdminAssignmentResponse(
            assignment.Id!,
            assignment.Title,
            assignment.Description,
            assignment.CourseId,
            courseNames.GetValueOrDefault(assignment.CourseId, "Unknown course"),
            assignment.SubjectId,
            subjectNames.GetValueOrDefault(assignment.SubjectId, "Unknown subject"),
            assignment.TeacherId,
            teacherNames.GetValueOrDefault(assignment.TeacherId, "Unknown teacher"),
            assignment.Deadline,
            assignment.MaximumMarks,
            assignment.Status,
            submissionCounts.GetValueOrDefault(assignment.Id!, 0),
            assignment.CreatedAt,
            assignment.UpdatedAt)));
    }

    [HttpGet("submissions")]
    public async Task<ActionResult<IReadOnlyList<AdminSubmissionResponse>>> GetSubmissions(
        [FromQuery] string? assignmentId,
        [FromQuery] SubmissionStatus? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = Builders<Submission>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(assignmentId))
        {
            if (!ObjectId.TryParse(assignmentId, out _))
            {
                return InvalidFilter("The assignment ID is invalid.");
            }

            filter &= Builders<Submission>.Filter.Eq(
                submission => submission.AssignmentId,
                assignmentId);
        }

        if (status is not null)
        {
            filter &= Builders<Submission>.Filter.Eq(submission => submission.Status, status.Value);
        }

        var submissions = await database.Submissions.Find(filter)
            .SortByDescending(submission => submission.SubmittedAt)
            .ToListAsync(cancellationToken);
        var assignments = await database.Assignments.Find(Builders<Assignment>.Filter.Empty)
            .ToListAsync(cancellationToken);
        var students = await database.Users.Find(user => user.Role == UserRole.Student)
            .ToListAsync(cancellationToken);
        var assignmentMap = assignments.ToDictionary(assignment => assignment.Id!);
        var studentMap = students.ToDictionary(student => student.Id!);

        var responses = submissions.Select(submission =>
        {
            assignmentMap.TryGetValue(submission.AssignmentId, out var assignment);
            studentMap.TryGetValue(submission.StudentId, out var student);

            return new AdminSubmissionResponse(
                submission.Id!,
                submission.AssignmentId,
                assignment?.Title ?? "Unknown assignment",
                submission.StudentId,
                student?.FullName ?? "Unknown student",
                student?.Email ?? string.Empty,
                submission.Answer,
                submission.Status,
                submission.Marks,
                assignment?.MaximumMarks ?? 0,
                submission.Feedback,
                submission.SubmittedAt,
                submission.ReviewedAt,
                submission.UpdatedAt);
        });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            responses = responses.Where(response =>
                response.AssignmentTitle.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                response.StudentName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                response.StudentEmail.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(responses);
    }

    private ObjectResult InvalidFilter(string detail) =>
        Problem(title: "Invalid filter", detail: detail, statusCode: StatusCodes.Status400BadRequest);
}
