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
[Route("api/teacher/submissions")]
public sealed class TeacherSubmissionsController(MongoDbContext database) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeacherSubmissionResponse>>> GetAll(
        [FromQuery] string? assignmentId,
        [FromQuery] SubmissionStatus? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var assignments = await database.Assignments.Find(
                assignment => assignment.TeacherId == GetTeacherId())
            .ToListAsync(cancellationToken);
        var assignmentIds = assignments.Select(assignment => assignment.Id!).ToArray();

        if (!string.IsNullOrWhiteSpace(assignmentId))
        {
            if (!ObjectId.TryParse(assignmentId, out _))
            {
                return InvalidRequest("The assignment ID is invalid.");
            }

            if (!assignmentIds.Contains(assignmentId))
            {
                return InvalidRequest("The selected assignment does not belong to you.");
            }

            assignmentIds = [assignmentId];
        }

        if (assignmentIds.Length == 0)
        {
            return Ok(Array.Empty<TeacherSubmissionResponse>());
        }

        var filter = Builders<Submission>.Filter.In(
            submission => submission.AssignmentId,
            assignmentIds);
        if (status is not null)
        {
            filter &= Builders<Submission>.Filter.Eq(submission => submission.Status, status.Value);
        }

        var submissions = await database.Submissions.Find(filter)
            .SortByDescending(submission => submission.SubmittedAt)
            .ToListAsync(cancellationToken);
        var responses = await ToResponses(submissions, assignments, cancellationToken);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            responses = responses.Where(response =>
                    response.AssignmentTitle.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    response.StudentName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    response.StudentEmail.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        return Ok(responses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TeacherSubmissionResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The submission ID is invalid.");
        }

        var submission = await database.Submissions.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (submission is null)
        {
            return SubmissionNotFound();
        }

        var assignment = await database.Assignments.Find(candidate =>
                candidate.Id == submission.AssignmentId &&
                candidate.TeacherId == GetTeacherId())
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return SubmissionNotFound();
        }

        return Ok((await ToResponses([submission], [assignment], cancellationToken)).Single());
    }

    [HttpPut("{id}/review")]
    public async Task<ActionResult<TeacherSubmissionResponse>> Review(
        string id,
        ReviewSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The submission ID is invalid.");
        }

        var submission = await database.Submissions.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (submission is null)
        {
            return SubmissionNotFound();
        }

        var assignment = await database.Assignments.Find(candidate =>
                candidate.Id == submission.AssignmentId &&
                candidate.TeacherId == GetTeacherId())
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return SubmissionNotFound();
        }

        if (request.Marks > assignment.MaximumMarks)
        {
            return InvalidRequest(
                $"Marks cannot exceed the assignment maximum of {assignment.MaximumMarks}.");
        }

        if (request.Status == SubmissionStatus.Reviewed && request.Marks is null)
        {
            return InvalidRequest("A mark is required when a submission is set to Reviewed.");
        }

        submission.Marks = request.Marks;
        submission.Feedback = request.Feedback.Trim();
        submission.Status = request.Status;
        submission.ReviewedAt = request.Status is SubmissionStatus.Reviewed or SubmissionStatus.Returned
            ? DateTime.UtcNow
            : null;
        submission.UpdatedAt = DateTime.UtcNow;

        await database.Submissions.ReplaceOneAsync(
            candidate => candidate.Id == id,
            submission,
            cancellationToken: cancellationToken);

        return Ok((await ToResponses([submission], [assignment], cancellationToken)).Single());
    }

    private string GetTeacherId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated teacher ID is missing.");

    private async Task<IReadOnlyList<TeacherSubmissionResponse>> ToResponses(
        IReadOnlyCollection<Submission> submissions,
        IReadOnlyCollection<Assignment> assignments,
        CancellationToken cancellationToken)
    {
        if (submissions.Count == 0)
        {
            return [];
        }

        var studentIds = submissions.Select(submission => submission.StudentId).Distinct().ToArray();
        var students = await database.Users.Find(
                Builders<User>.Filter.In(student => student.Id, studentIds))
            .ToListAsync(cancellationToken);
        var studentMap = students.ToDictionary(student => student.Id!);
        var assignmentMap = assignments.ToDictionary(assignment => assignment.Id!);

        return submissions.Select(submission =>
        {
            studentMap.TryGetValue(submission.StudentId, out var student);
            assignmentMap.TryGetValue(submission.AssignmentId, out var assignment);

            return new TeacherSubmissionResponse(
                submission.Id!,
                submission.AssignmentId,
                assignment?.Title ?? "Unknown assignment",
                submission.StudentId,
                student?.FullName ?? "Unknown student",
                student?.Email ?? string.Empty,
                submission.Answer,
                submission.PdfFileName,
                submission.PdfFileSize,
                submission.Status,
                submission.Marks,
                assignment?.MaximumMarks ?? 0,
                submission.Feedback,
                submission.SubmittedAt,
                submission.ReviewedAt,
                submission.UpdatedAt);
        }).ToArray();
    }

    private ObjectResult InvalidRequest(string detail) =>
        Problem(title: "Invalid submission request", detail: detail, statusCode: 400);

    private ObjectResult SubmissionNotFound() =>
        Problem(
            title: "Submission not found",
            detail: "The submission does not exist or is not attached to one of your assignments.",
            statusCode: 404);
}
