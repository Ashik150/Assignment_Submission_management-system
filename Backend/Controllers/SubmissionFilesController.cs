using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Rules;
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
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return false;
        }

        if (role != UserRole.Teacher)
        {
            return SubmissionAuthorizationRules.CanDownloadPdf(role, userId, submission);
        }

        var assignment = await database.Assignments.Find(candidate =>
                candidate.Id == submission.AssignmentId)
            .FirstOrDefaultAsync(cancellationToken);
        return SubmissionAuthorizationRules.CanDownloadPdf(role, userId, submission, assignment);
    }
}
