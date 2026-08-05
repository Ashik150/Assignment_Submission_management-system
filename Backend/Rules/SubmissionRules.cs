using Backend.Models;

namespace Backend.Rules;

public enum SubmissionUpdateFailure
{
    None,
    AssignmentUnavailable,
    DeadlinePassed,
    ReviewedSubmission
}

public enum SubmissionReviewFailure
{
    None,
    MarksBelowZero,
    MarksExceedMaximum,
    ReviewedMarksRequired
}

public static class SubmissionRules
{
    public static bool CanStudentAccessAssignment(User student, Assignment assignment) =>
        student.Role == UserRole.Student &&
        student.IsActive &&
        student.CourseId is not null &&
        assignment.CourseId == student.CourseId &&
        assignment.Status == AssignmentStatus.Published;

    public static bool CanCreateSubmission(User student, Assignment assignment, DateTime utcNow) =>
        CanStudentAccessAssignment(student, assignment) && assignment.Deadline > utcNow;

    public static SubmissionUpdateFailure GetUpdateFailure(
        Assignment assignment,
        Submission submission,
        DateTime utcNow)
    {
        if (assignment.Status != AssignmentStatus.Published ||
            assignment.Id != submission.AssignmentId)
        {
            return SubmissionUpdateFailure.AssignmentUnavailable;
        }

        if (assignment.Deadline <= utcNow)
        {
            return SubmissionUpdateFailure.DeadlinePassed;
        }

        return submission.Status == SubmissionStatus.Reviewed
            ? SubmissionUpdateFailure.ReviewedSubmission
            : SubmissionUpdateFailure.None;
    }

    public static bool HasSubmissionContent(
        string? answer,
        bool hasNewPdf,
        bool keepsExistingPdf) =>
        !string.IsNullOrWhiteSpace(answer) || hasNewPdf || keepsExistingPdf;

    public static SubmissionReviewFailure GetReviewFailure(
        Assignment assignment,
        decimal? marks,
        SubmissionStatus status)
    {
        if (marks < 0)
        {
            return SubmissionReviewFailure.MarksBelowZero;
        }

        if (marks > assignment.MaximumMarks)
        {
            return SubmissionReviewFailure.MarksExceedMaximum;
        }

        return status == SubmissionStatus.Reviewed && marks is null
            ? SubmissionReviewFailure.ReviewedMarksRequired
            : SubmissionReviewFailure.None;
    }

    public static void MarkAsResubmitted(Submission submission, DateTime utcNow)
    {
        submission.Status = SubmissionStatus.Submitted;
        submission.Marks = null;
        submission.ReviewedAt = null;
        submission.UpdatedAt = utcNow;
    }

    public static void ApplyReview(
        Submission submission,
        decimal? marks,
        string feedback,
        SubmissionStatus status,
        DateTime utcNow)
    {
        submission.Marks = marks;
        submission.Feedback = feedback.Trim();
        submission.Status = status;
        submission.ReviewedAt = status is SubmissionStatus.Reviewed or SubmissionStatus.Returned
            ? utcNow
            : null;
        submission.UpdatedAt = utcNow;
    }
}
