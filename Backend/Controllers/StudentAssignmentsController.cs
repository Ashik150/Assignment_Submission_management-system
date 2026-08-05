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
[Authorize(Roles = nameof(UserRole.Student))]
[Route("api/student")]
public sealed class StudentAssignmentsController(MongoDbContext database) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<StudentDashboardResponse>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var enrollment = await GetEnrollment(cancellationToken);
        if (enrollment.Error is not null)
        {
            return enrollment.Error;
        }

        var student = enrollment.Student!;
        var course = enrollment.Course!;

        var assignments = await database.Assignments.Find(assignment =>
                assignment.CourseId == course.Id &&
                assignment.Status == AssignmentStatus.Published)
            .Project(assignment => new { assignment.Id, assignment.Deadline })
            .ToListAsync(cancellationToken);
        var assignmentIds = assignments.Select(assignment => assignment.Id!).ToArray();
        var submissions = assignmentIds.Length == 0
            ? []
            : await database.Submissions.Find(
                    Builders<Submission>.Filter.Eq(
                        submission => submission.StudentId,
                        student.Id) &
                    Builders<Submission>.Filter.In(
                        submission => submission.AssignmentId,
                        assignmentIds))
                .Project(submission => submission.AssignmentId)
                .ToListAsync(cancellationToken);
        var submittedAssignmentIds = submissions.ToHashSet();
        var now = DateTime.UtcNow;
        var weekFromNow = now.AddDays(7);

        return Ok(new StudentDashboardResponse(
            course.Id!,
            course.Name,
            assignments.Count,
            assignments.LongCount(assignment =>
                assignment.Deadline >= now && assignment.Deadline <= weekFromNow),
            submittedAssignmentIds.Count,
            assignments.LongCount(assignment =>
                assignment.Deadline > now && !submittedAssignmentIds.Contains(assignment.Id!))));
    }

    [HttpGet("assignments")]
    public async Task<ActionResult<IReadOnlyList<StudentAssignmentResponse>>> GetAssignments(
        [FromQuery] string? subjectId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var enrollment = await GetEnrollment(cancellationToken);
        if (enrollment.Error is not null)
        {
            return enrollment.Error;
        }

        var filter = Builders<Assignment>.Filter.Eq(
                assignment => assignment.CourseId,
                enrollment.Course!.Id) &
            Builders<Assignment>.Filter.Eq(
                assignment => assignment.Status,
                AssignmentStatus.Published);

        if (!string.IsNullOrWhiteSpace(subjectId))
        {
            if (!ObjectId.TryParse(subjectId, out _))
            {
                return InvalidRequest("The subject ID is invalid.");
            }

            filter &= Builders<Assignment>.Filter.Eq(assignment => assignment.SubjectId, subjectId);
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
            .SortBy(assignment => assignment.Deadline)
            .ToListAsync(cancellationToken);
        return Ok(await ToResponses(
            assignments,
            enrollment.Student!,
            enrollment.Course!,
            cancellationToken));
    }

    [HttpGet("assignments/{id}")]
    public async Task<ActionResult<StudentAssignmentResponse>> GetAssignment(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The assignment ID is invalid.");
        }

        var enrollment = await GetEnrollment(cancellationToken);
        if (enrollment.Error is not null)
        {
            return enrollment.Error;
        }

        var assignment = await database.Assignments.Find(candidate =>
                candidate.Id == id &&
                candidate.CourseId == enrollment.Course!.Id &&
                candidate.Status == AssignmentStatus.Published)
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return AssignmentNotFound();
        }

        return Ok((await ToResponses(
            [assignment],
            enrollment.Student!,
            enrollment.Course!,
            cancellationToken)).Single());
    }

    private string GetStudentId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated student ID is missing.");

    private async Task<(User? Student, Course? Course, ObjectResult? Error)> GetEnrollment(
        CancellationToken cancellationToken)
    {
        var student = await database.Users.Find(user =>
                user.Id == GetStudentId() &&
                user.Role == UserRole.Student &&
                user.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
        if (student is null)
        {
            return (null, null, Problem(
                title: "Student account unavailable",
                detail: "The student account is inactive or no longer exists.",
                statusCode: 403));
        }

        if (student.CourseId is null)
        {
            return (student, null, EnrollmentRequired());
        }

        var course = await database.Courses.Find(candidate =>
                candidate.Id == student.CourseId && candidate.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
        return course is null
            ? (student, null, EnrollmentRequired())
            : (student, course, null);
    }

    private async Task<IReadOnlyList<StudentAssignmentResponse>> ToResponses(
        IReadOnlyCollection<Assignment> assignments,
        User student,
        Course course,
        CancellationToken cancellationToken)
    {
        if (assignments.Count == 0)
        {
            return [];
        }

        var subjectIds = assignments.Select(assignment => assignment.SubjectId).Distinct().ToArray();
        var teacherIds = assignments.Select(assignment => assignment.TeacherId).Distinct().ToArray();
        var assignmentIds = assignments.Select(assignment => assignment.Id!).ToArray();
        var subjects = await database.Subjects.Find(
                Builders<Subject>.Filter.In(subject => subject.Id, subjectIds))
            .ToListAsync(cancellationToken);
        var teachers = await database.Users.Find(
                Builders<User>.Filter.In(teacher => teacher.Id, teacherIds))
            .ToListAsync(cancellationToken);
        var submissions = await database.Submissions.Find(
                Builders<Submission>.Filter.Eq(submission => submission.StudentId, student.Id) &
                Builders<Submission>.Filter.In(submission => submission.AssignmentId, assignmentIds))
            .ToListAsync(cancellationToken);
        var subjectNames = subjects.ToDictionary(subject => subject.Id!, subject => subject.Name);
        var teacherNames = teachers.ToDictionary(teacher => teacher.Id!, teacher => teacher.FullName);
        var submissionMap = submissions.ToDictionary(submission => submission.AssignmentId);
        var now = DateTime.UtcNow;

        return assignments.Select(assignment =>
        {
            submissionMap.TryGetValue(assignment.Id!, out var submission);
            var canUpdate = submission is not null &&
                assignment.Deadline > now &&
                submission.Status != SubmissionStatus.Reviewed;

            return new StudentAssignmentResponse(
                assignment.Id!,
                assignment.Title,
                assignment.Description,
                assignment.CourseId,
                course.Name,
                assignment.SubjectId,
                subjectNames.GetValueOrDefault(assignment.SubjectId, "Unknown subject"),
                assignment.TeacherId,
                teacherNames.GetValueOrDefault(assignment.TeacherId, "Unknown teacher"),
                assignment.Deadline,
                assignment.MaximumMarks,
                assignment.CreatedAt,
                submission?.Id,
                submission?.Status,
                submission?.Marks,
                submission?.Feedback,
                submission is null && assignment.Deadline > now,
                canUpdate);
        }).ToArray();
    }

    private ObjectResult InvalidRequest(string detail) =>
        Problem(title: "Invalid assignment request", detail: detail, statusCode: 400);

    private ObjectResult EnrollmentRequired() =>
        Problem(
            title: "Course enrollment required",
            detail: "Ask an administrator to assign your account to an active course.",
            statusCode: 409);

    private ObjectResult AssignmentNotFound() =>
        Problem(
            title: "Assignment not found",
            detail: "The assignment is not published or is not assigned to your course.",
            statusCode: 404);
}
