using Backend.Models;

namespace Backend.Rules;

public static class SubmissionAuthorizationRules
{
    public static bool CanDownloadPdf(
        UserRole role,
        string? userId,
        Submission submission,
        Assignment? assignment = null) =>
        role switch
        {
            UserRole.Admin => true,
            UserRole.Student => submission.StudentId == userId,
            UserRole.Teacher => assignment is not null &&
                assignment.Id == submission.AssignmentId &&
                assignment.TeacherId == userId,
            _ => false
        };
}
