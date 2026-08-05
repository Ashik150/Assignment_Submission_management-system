using System.Security.Claims;
using Backend.Contracts;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Backend.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Teacher))]
[Route("api/teacher")]
public sealed class TeacherAssignmentsController(MongoDbContext database) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<TeacherDashboardResponse>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherId();
        var assignments = await database.Assignments.Find(assignment => assignment.TeacherId == teacherId)
            .Project(assignment => new { assignment.Id, assignment.Status })
            .ToListAsync(cancellationToken);
        var assignmentIds = assignments.Select(assignment => assignment.Id!).ToArray();

        var assignedSubjectsTask = database.Subjects.CountDocumentsAsync(
            subject => subject.TeacherId == teacherId,
            cancellationToken: cancellationToken);
        var submissionsFilter = Builders<Submission>.Filter.In(
            submission => submission.AssignmentId,
            assignmentIds);
        var submissionsTask = assignmentIds.Length == 0
            ? Task.FromResult(0L)
            : database.Submissions.CountDocumentsAsync(
                submissionsFilter,
                cancellationToken: cancellationToken);
        var pendingTask = assignmentIds.Length == 0
            ? Task.FromResult(0L)
            : database.Submissions.CountDocumentsAsync(
                submissionsFilter & Builders<Submission>.Filter.In(
                    submission => submission.Status,
                    new[] { SubmissionStatus.Submitted, SubmissionStatus.Late }),
                cancellationToken: cancellationToken);

        await Task.WhenAll(assignedSubjectsTask, submissionsTask, pendingTask);

        return Ok(new TeacherDashboardResponse(
            await assignedSubjectsTask,
            assignments.Count,
            assignments.LongCount(assignment => assignment.Status == AssignmentStatus.Published),
            await submissionsTask,
            await pendingTask));
    }

    [HttpGet("subjects")]
    public async Task<ActionResult<IReadOnlyList<TeacherSubjectResponse>>> GetAssignedSubjects(
        CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherId();
        var subjects = await database.Subjects.Find(subject => subject.TeacherId == teacherId)
            .SortBy(subject => subject.Name)
            .ToListAsync(cancellationToken);
        var courseIds = subjects.Select(subject => subject.CourseId).Distinct().ToArray();
        var courses = courseIds.Length == 0
            ? []
            : await database.Courses.Find(Builders<Course>.Filter.In(course => course.Id, courseIds))
                .ToListAsync(cancellationToken);
        var courseMap = courses.ToDictionary(course => course.Id!);

        return Ok(subjects.Select(subject =>
        {
            courseMap.TryGetValue(subject.CourseId, out var course);
            return new TeacherSubjectResponse(
                subject.Id!,
                subject.Name,
                subject.Code,
                subject.CourseId,
                course?.Name ?? "Unknown course",
                subject.IsActive,
                course?.IsActive ?? false);
        }));
    }

    [HttpGet("assignments")]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignmentResponse>>> GetAssignments(
        [FromQuery] AssignmentStatus? status,
        [FromQuery] string? courseId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = Builders<Assignment>.Filter.Eq(
            assignment => assignment.TeacherId,
            GetTeacherId());

        if (status is not null)
        {
            filter &= Builders<Assignment>.Filter.Eq(assignment => assignment.Status, status.Value);
        }

        if (!string.IsNullOrWhiteSpace(courseId))
        {
            if (!ObjectId.TryParse(courseId, out _))
            {
                return InvalidRequest("The course ID is invalid.");
            }

            filter &= Builders<Assignment>.Filter.Eq(assignment => assignment.CourseId, courseId);
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
        return Ok(await ToResponses(assignments, cancellationToken));
    }

    [HttpGet("assignments/{id}")]
    public async Task<ActionResult<TeacherAssignmentResponse>> GetAssignment(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The assignment ID is invalid.");
        }

        var assignment = await database.Assignments.Find(candidate =>
                candidate.Id == id && candidate.TeacherId == GetTeacherId())
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return AssignmentNotFound();
        }

        return Ok((await ToResponses([assignment], cancellationToken)).Single());
    }

    [HttpPost("assignments")]
    public async Task<ActionResult<TeacherAssignmentResponse>> CreateAssignment(
        CreateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Deadline.ToUniversalTime() <= DateTime.UtcNow)
        {
            return InvalidRequest("The assignment deadline must be in the future.");
        }

        var relation = await ResolveAssignedSubject(
            request.CourseId,
            request.SubjectId,
            cancellationToken);
        if (relation.Error is not null)
        {
            return relation.Error;
        }

        var assignment = new Assignment
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CourseId = request.CourseId,
            SubjectId = request.SubjectId,
            TeacherId = GetTeacherId(),
            Deadline = request.Deadline.ToUniversalTime(),
            MaximumMarks = request.MaximumMarks,
            Status = request.Status
        };
        await database.Assignments.InsertOneAsync(assignment, cancellationToken: cancellationToken);

        var response = ToResponse(
            assignment,
            relation.Course!.Name,
            relation.Subject!.Name,
            0);
        return CreatedAtAction(nameof(GetAssignment), new { id = assignment.Id }, response);
    }

    [HttpPut("assignments/{id}")]
    public async Task<ActionResult<TeacherAssignmentResponse>> UpdateAssignment(
        string id,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The assignment ID is invalid.");
        }

        var assignment = await database.Assignments.Find(candidate =>
                candidate.Id == id && candidate.TeacherId == GetTeacherId())
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return AssignmentNotFound();
        }

        var deadline = request.Deadline.ToUniversalTime();
        if (deadline != assignment.Deadline && deadline <= DateTime.UtcNow)
        {
            return InvalidRequest("A changed assignment deadline must be in the future.");
        }

        var relation = await ResolveAssignedSubject(
            request.CourseId,
            request.SubjectId,
            cancellationToken);
        if (relation.Error is not null)
        {
            return relation.Error;
        }

        var hasSubmissions = await database.Submissions.Find(
                submission => submission.AssignmentId == id)
            .AnyAsync(cancellationToken);
        if (hasSubmissions &&
            (assignment.CourseId != request.CourseId || assignment.SubjectId != request.SubjectId))
        {
            return ConflictProblem(
                "The course or subject cannot be changed after students have submitted work.");
        }

        assignment.Title = request.Title.Trim();
        assignment.Description = request.Description.Trim();
        assignment.CourseId = request.CourseId;
        assignment.SubjectId = request.SubjectId;
        assignment.Deadline = deadline;
        assignment.MaximumMarks = request.MaximumMarks;
        assignment.Status = request.Status;
        assignment.UpdatedAt = DateTime.UtcNow;

        await database.Assignments.ReplaceOneAsync(
            candidate => candidate.Id == id && candidate.TeacherId == GetTeacherId(),
            assignment,
            cancellationToken: cancellationToken);
        var submissionCount = await database.Submissions.CountDocumentsAsync(
            submission => submission.AssignmentId == id,
            cancellationToken: cancellationToken);

        return Ok(ToResponse(
            assignment,
            relation.Course!.Name,
            relation.Subject!.Name,
            checked((int)submissionCount)));
    }

    [HttpDelete("assignments/{id}")]
    public async Task<IActionResult> DeleteAssignment(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The assignment ID is invalid.");
        }

        var assignment = await database.Assignments.Find(candidate =>
                candidate.Id == id && candidate.TeacherId == GetTeacherId())
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return AssignmentNotFound();
        }

        var hasSubmissions = await database.Submissions.Find(
                submission => submission.AssignmentId == id)
            .AnyAsync(cancellationToken);
        if (hasSubmissions)
        {
            return ConflictProblem(
                "This assignment has submissions and cannot be deleted. Keep it for academic history.");
        }

        await database.Assignments.DeleteOneAsync(
            candidate => candidate.Id == id && candidate.TeacherId == GetTeacherId(),
            cancellationToken);
        return NoContent();
    }

    private string GetTeacherId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated teacher ID is missing.");

    private async Task<(Course? Course, Subject? Subject, ObjectResult? Error)> ResolveAssignedSubject(
        string courseId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(courseId, out _) || !ObjectId.TryParse(subjectId, out _))
        {
            return (null, null, InvalidRequest("The course or subject ID is invalid."));
        }

        var course = await database.Courses.Find(candidate => candidate.Id == courseId)
            .FirstOrDefaultAsync(cancellationToken);
        var subject = await database.Subjects.Find(candidate =>
                candidate.Id == subjectId &&
                candidate.CourseId == courseId &&
                candidate.TeacherId == GetTeacherId())
            .FirstOrDefaultAsync(cancellationToken);

        if (course is null || subject is null)
        {
            return (null, null, InvalidRequest(
                "The selected subject is not assigned to you or does not belong to this course."));
        }

        if (!course.IsActive || !subject.IsActive)
        {
            return (null, null, InvalidRequest(
                "Assignments can only be created for active courses and subjects."));
        }

        return (course, subject, null);
    }

    private async Task<IReadOnlyList<TeacherAssignmentResponse>> ToResponses(
        IReadOnlyCollection<Assignment> assignments,
        CancellationToken cancellationToken)
    {
        if (assignments.Count == 0)
        {
            return [];
        }

        var courseIds = assignments.Select(assignment => assignment.CourseId).Distinct().ToArray();
        var subjectIds = assignments.Select(assignment => assignment.SubjectId).Distinct().ToArray();
        var assignmentIds = assignments.Select(assignment => assignment.Id!).ToArray();
        var courses = await database.Courses.Find(
                Builders<Course>.Filter.In(course => course.Id, courseIds))
            .ToListAsync(cancellationToken);
        var subjects = await database.Subjects.Find(
                Builders<Subject>.Filter.In(subject => subject.Id, subjectIds))
            .ToListAsync(cancellationToken);
        var submissionIds = await database.Submissions.Find(
                Builders<Submission>.Filter.In(submission => submission.AssignmentId, assignmentIds))
            .Project(submission => submission.AssignmentId)
            .ToListAsync(cancellationToken);
        var courseMap = courses.ToDictionary(course => course.Id!, course => course.Name);
        var subjectMap = subjects.ToDictionary(subject => subject.Id!, subject => subject.Name);
        var submissionCounts = submissionIds.GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());

        return assignments.Select(assignment => ToResponse(
            assignment,
            courseMap.GetValueOrDefault(assignment.CourseId, "Unknown course"),
            subjectMap.GetValueOrDefault(assignment.SubjectId, "Unknown subject"),
            submissionCounts.GetValueOrDefault(assignment.Id!, 0))).ToArray();
    }

    private static TeacherAssignmentResponse ToResponse(
        Assignment assignment,
        string courseName,
        string subjectName,
        int submissionCount) => new(
            assignment.Id!,
            assignment.Title,
            assignment.Description,
            assignment.CourseId,
            courseName,
            assignment.SubjectId,
            subjectName,
            assignment.Deadline,
            assignment.MaximumMarks,
            assignment.Status,
            submissionCount,
            assignment.CreatedAt,
            assignment.UpdatedAt);

    private ObjectResult InvalidRequest(string detail) =>
        Problem(title: "Invalid assignment request", detail: detail, statusCode: 400);

    private ObjectResult AssignmentNotFound() =>
        Problem(
            title: "Assignment not found",
            detail: "The assignment does not exist or does not belong to you.",
            statusCode: 404);

    private ObjectResult ConflictProblem(string detail) =>
        Problem(title: "Assignment conflict", detail: detail, statusCode: 409);
}
