using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/submissions")]
public sealed class SubmissionFilesController(
    MongoDbContext database,
    SubmissionPdfService pdfService) : ControllerBase
{
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> Download(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return Problem(
                title: "Invalid submission request",
                detail: "The submission ID is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var submission = await database.Submissions.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (submission?.PdfFileId is null || submission.PdfFileName is null)
        {
            return NotFound();
        }

        if (!await CanAccess(submission, cancellationToken))
        {
            return Forbid();
        }

        try
        {
            var bytes = await pdfService.Download(submission.PdfFileId, cancellationToken);
            return File(bytes, "application/pdf", submission.PdfFileName);
        }
        catch (GridFSFileNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<bool> CanAccess(Submission submission, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (role == nameof(UserRole.Admin))
        {
            return true;
        }

        if (role == nameof(UserRole.Student))
        {
            return submission.StudentId == userId;
        }

        if (role != nameof(UserRole.Teacher))
        {
            return false;
        }

        return await database.Assignments.Find(assignment =>
                assignment.Id == submission.AssignmentId && assignment.TeacherId == userId)
            .AnyAsync(cancellationToken);
    }
}
