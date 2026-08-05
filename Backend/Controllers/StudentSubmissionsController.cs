using System.Security.Claims;
using Backend.Contracts;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Backend.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Student))]
[Route("api/student")]
public sealed class StudentSubmissionsController(
    MongoDbContext database,
    SubmissionPdfService pdfService) : ControllerBase
{
    [HttpGet("submissions")]
    public async Task<ActionResult<IReadOnlyList<StudentSubmissionResponse>>> GetAll(
        [FromQuery] SubmissionStatus? status,
        CancellationToken cancellationToken)
    {
        var student = await GetStudent(cancellationToken);
        if (student is null)
        {
            return StudentUnavailable();
        }

        var filter = Builders<Submission>.Filter.Eq(
            submission => submission.StudentId,
            student.Id);
        if (status is not null)
        {
            filter &= Builders<Submission>.Filter.Eq(submission => submission.Status, status.Value);
        }

        var submissions = await database.Submissions.Find(filter)
            .SortByDescending(submission => submission.UpdatedAt)
            .ToListAsync(cancellationToken);
        return Ok(await ToResponses(submissions, cancellationToken));
    }

    [HttpGet("submissions/{id}")]
    public async Task<ActionResult<StudentSubmissionResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The submission ID is invalid.");
        }

        var student = await GetStudent(cancellationToken);
        if (student is null)
        {
            return StudentUnavailable();
        }

        var submission = await database.Submissions.Find(candidate =>
                candidate.Id == id && candidate.StudentId == student.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (submission is null)
        {
            return SubmissionNotFound();
        }

        return Ok((await ToResponses([submission], cancellationToken)).Single());
    }

    [HttpPost("assignments/{assignmentId}/submission")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StudentSubmissionResponse>> Submit(
        string assignmentId,
        [FromForm] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(assignmentId, out _))
        {
            return InvalidRequest("The assignment ID is invalid.");
        }

        var student = await GetStudent(cancellationToken);
        if (student is null)
        {
            return StudentUnavailable();
        }

        if (student.CourseId is null)
        {
            return EnrollmentRequired();
        }

        var assignment = await database.Assignments.Find(candidate =>
                candidate.Id == assignmentId &&
                candidate.CourseId == student.CourseId &&
                candidate.Status == AssignmentStatus.Published)
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return AssignmentNotFound();
        }

        if (assignment.Deadline <= DateTime.UtcNow)
        {
            return ConflictProblem("The deadline has passed and this assignment no longer accepts submissions.");
        }

        var answer = request.Answer?.Trim() ?? string.Empty;
        if (answer.Length == 0 && request.Pdf is null)
        {
            return InvalidRequest("Write an answer, attach a PDF, or provide both.");
        }

        StoredSubmissionPdf? uploadedPdf = null;
        if (request.Pdf is not null)
        {
            try
            {
                uploadedPdf = await pdfService.Upload(request.Pdf, cancellationToken);
            }
            catch (InvalidDataException exception)
            {
                return InvalidRequest(exception.Message);
            }
        }

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = student.Id!,
            Answer = answer,
            PdfFileId = uploadedPdf?.FileId,
            PdfFileName = uploadedPdf?.FileName,
            PdfFileSize = uploadedPdf?.FileSize,
            Status = SubmissionStatus.Submitted
        };

        try
        {
            await database.Submissions.InsertOneAsync(submission, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            await pdfService.Delete(uploadedPdf?.FileId, cancellationToken);
            return ConflictProblem("You have already submitted an answer for this assignment.");
        }
        catch
        {
            await pdfService.Delete(uploadedPdf?.FileId, cancellationToken);
            throw;
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = submission.Id },
            (await ToResponses([submission], cancellationToken)).Single());
    }

    [HttpPut("submissions/{id}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StudentSubmissionResponse>> Update(
        string id,
        [FromForm] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return InvalidRequest("The submission ID is invalid.");
        }

        var student = await GetStudent(cancellationToken);
        if (student is null)
        {
            return StudentUnavailable();
        }

        var submission = await database.Submissions.Find(candidate =>
                candidate.Id == id && candidate.StudentId == student.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (submission is null)
        {
            return SubmissionNotFound();
        }

        var assignment = await database.Assignments.Find(candidate =>
                candidate.Id == submission.AssignmentId &&
                candidate.Status == AssignmentStatus.Published)
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return AssignmentNotFound();
        }

        if (assignment.Deadline <= DateTime.UtcNow)
        {
            return ConflictProblem("The deadline has passed and this submission can no longer be updated.");
        }

        if (submission.Status == SubmissionStatus.Reviewed)
        {
            return ConflictProblem("A reviewed submission cannot be updated.");
        }

        var answer = request.Answer?.Trim() ?? string.Empty;
        var keepsExistingPdf = request.Pdf is null && !request.RemovePdf && submission.PdfFileId is not null;
        if (answer.Length == 0 && request.Pdf is null && !keepsExistingPdf)
        {
            return InvalidRequest("Write an answer, attach a PDF, or provide both.");
        }

        StoredSubmissionPdf? uploadedPdf = null;
        if (request.Pdf is not null)
        {
            try
            {
                uploadedPdf = await pdfService.Upload(request.Pdf, cancellationToken);
            }
            catch (InvalidDataException exception)
            {
                return InvalidRequest(exception.Message);
            }
        }

        var previousPdfId = submission.PdfFileId;
        submission.Answer = answer;
        if (uploadedPdf is not null)
        {
            submission.PdfFileId = uploadedPdf.FileId;
            submission.PdfFileName = uploadedPdf.FileName;
            submission.PdfFileSize = uploadedPdf.FileSize;
        }
        else if (request.RemovePdf)
        {
            submission.PdfFileId = null;
            submission.PdfFileName = null;
            submission.PdfFileSize = null;
        }

        submission.Status = SubmissionStatus.Submitted;
        submission.Marks = null;
        submission.ReviewedAt = null;
        submission.UpdatedAt = DateTime.UtcNow;
        try
        {
            await database.Submissions.ReplaceOneAsync(
                candidate => candidate.Id == id && candidate.StudentId == student.Id,
                submission,
                cancellationToken: cancellationToken);
        }
        catch
        {
            await pdfService.Delete(uploadedPdf?.FileId, cancellationToken);
            throw;
        }

        if (previousPdfId is not null && previousPdfId != submission.PdfFileId)
        {
            await pdfService.Delete(previousPdfId, cancellationToken);
        }

        return Ok((await ToResponses([submission], cancellationToken)).Single());
    }

    private string GetStudentId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated student ID is missing.");

    private async Task<User?> GetStudent(CancellationToken cancellationToken) =>
        await database.Users.Find(user =>
                user.Id == GetStudentId() &&
                user.Role == UserRole.Student &&
                user.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<StudentSubmissionResponse>> ToResponses(
        IReadOnlyCollection<Submission> submissions,
        CancellationToken cancellationToken)
    {
        if (submissions.Count == 0)
        {
            return [];
        }

        var assignmentIds = submissions.Select(submission => submission.AssignmentId).Distinct().ToArray();
        var assignments = await database.Assignments.Find(
                Builders<Assignment>.Filter.In(assignment => assignment.Id, assignmentIds))
            .ToListAsync(cancellationToken);
        var subjectIds = assignments.Select(assignment => assignment.SubjectId).Distinct().ToArray();
        var subjects = await database.Subjects.Find(
                Builders<Subject>.Filter.In(subject => subject.Id, subjectIds))
            .ToListAsync(cancellationToken);
        var assignmentMap = assignments.ToDictionary(assignment => assignment.Id!);
        var subjectNames = subjects.ToDictionary(subject => subject.Id!, subject => subject.Name);
        var now = DateTime.UtcNow;

        return submissions.Select(submission =>
        {
            assignmentMap.TryGetValue(submission.AssignmentId, out var assignment);
            var canUpdate = assignment is not null &&
                assignment.Status == AssignmentStatus.Published &&
                assignment.Deadline > now &&
                submission.Status != SubmissionStatus.Reviewed;

            return new StudentSubmissionResponse(
                submission.Id!,
                submission.AssignmentId,
                assignment?.Title ?? "Unknown assignment",
                assignment?.SubjectId ?? string.Empty,
                assignment is null
                    ? "Unknown subject"
                    : subjectNames.GetValueOrDefault(assignment.SubjectId, "Unknown subject"),
                submission.Answer,
                submission.PdfFileName,
                submission.PdfFileSize,
                submission.Status,
                submission.Marks,
                assignment?.MaximumMarks ?? 0,
                submission.Feedback,
                assignment?.Deadline ?? submission.SubmittedAt,
                submission.SubmittedAt,
                submission.ReviewedAt,
                submission.UpdatedAt,
                canUpdate);
        }).ToArray();
    }

    private ObjectResult InvalidRequest(string detail) =>
        Problem(title: "Invalid submission request", detail: detail, statusCode: 400);

    private ObjectResult StudentUnavailable() =>
        Problem(
            title: "Student account unavailable",
            detail: "The student account is inactive or no longer exists.",
            statusCode: 403);

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

    private ObjectResult SubmissionNotFound() =>
        Problem(
            title: "Submission not found",
            detail: "The submission does not exist or does not belong to you.",
            statusCode: 404);

    private ObjectResult ConflictProblem(string detail) =>
        Problem(title: "Submission conflict", detail: detail, statusCode: 409);
}
