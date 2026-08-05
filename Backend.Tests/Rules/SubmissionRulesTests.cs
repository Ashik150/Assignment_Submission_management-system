using Backend.Models;
using Backend.Rules;

namespace Backend.Tests.Rules;

public sealed class SubmissionRulesTests
{
    private static readonly DateTime Now = new(2026, 8, 6, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MatchingActiveStudent_CanAccessPublishedCourseAssignment()
    {
        var student = Student();
        var assignment = Assignment();

        Assert.True(SubmissionRules.CanStudentAccessAssignment(student, assignment));
    }

    [Theory]
    [InlineData(false, UserRole.Student, "course-1", AssignmentStatus.Published)]
    [InlineData(true, UserRole.Teacher, "course-1", AssignmentStatus.Published)]
    [InlineData(true, UserRole.Student, "course-2", AssignmentStatus.Published)]
    [InlineData(true, UserRole.Student, "course-1", AssignmentStatus.Draft)]
    public void IneligibleStudent_CannotAccessAssignment(
        bool isActive,
        UserRole role,
        string courseId,
        AssignmentStatus status)
    {
        var student = Student();
        student.IsActive = isActive;
        student.Role = role;
        student.CourseId = courseId;
        var assignment = Assignment();
        assignment.Status = status;

        Assert.False(SubmissionRules.CanStudentAccessAssignment(student, assignment));
    }

    [Fact]
    public void Submission_CanBeCreatedOnlyBeforeDeadline()
    {
        var student = Student();
        var assignment = Assignment();
        assignment.Deadline = Now.AddMinutes(1);

        Assert.True(SubmissionRules.CanCreateSubmission(student, assignment, Now));
        assignment.Deadline = Now;
        Assert.False(SubmissionRules.CanCreateSubmission(student, assignment, Now));
    }

    [Theory]
    [InlineData("Written answer", false, false, true)]
    [InlineData("", true, false, true)]
    [InlineData("  ", false, true, true)]
    [InlineData("", false, false, false)]
    public void Submission_RequiresTextOrPdf(
        string answer,
        bool hasNewPdf,
        bool keepsExistingPdf,
        bool expected)
    {
        Assert.Equal(
            expected,
            SubmissionRules.HasSubmissionContent(answer, hasNewPdf, keepsExistingPdf));
    }

    [Fact]
    public void ReturnedSubmission_CanBeUpdatedBeforeDeadline()
    {
        var assignment = Assignment();
        var submission = Submission();
        submission.Status = SubmissionStatus.Returned;

        Assert.Equal(
            SubmissionUpdateFailure.None,
            SubmissionRules.GetUpdateFailure(assignment, submission, Now));
    }

    [Fact]
    public void ReviewedSubmission_CannotBeUpdated()
    {
        var submission = Submission();
        submission.Status = SubmissionStatus.Reviewed;
        var failure = SubmissionRules.GetUpdateFailure(Assignment(), submission, Now);

        Assert.Equal(SubmissionUpdateFailure.ReviewedSubmission, failure);
    }

    [Fact]
    public void Submission_CannotBeUpdatedAtOrAfterDeadline()
    {
        var assignment = Assignment();
        assignment.Deadline = Now;
        var failure = SubmissionRules.GetUpdateFailure(assignment, Submission(), Now);

        Assert.Equal(SubmissionUpdateFailure.DeadlinePassed, failure);
    }

    [Fact]
    public void DraftAssignmentSubmission_CannotBeUpdated()
    {
        var assignment = Assignment();
        assignment.Status = AssignmentStatus.Draft;
        var failure = SubmissionRules.GetUpdateFailure(assignment, Submission(), Now);

        Assert.Equal(SubmissionUpdateFailure.AssignmentUnavailable, failure);
    }

    [Theory]
    [InlineData(null, SubmissionStatus.Reviewed, SubmissionReviewFailure.ReviewedMarksRequired)]
    [InlineData(-1, SubmissionStatus.Submitted, SubmissionReviewFailure.MarksBelowZero)]
    [InlineData(101, SubmissionStatus.Reviewed, SubmissionReviewFailure.MarksExceedMaximum)]
    [InlineData(null, SubmissionStatus.Returned, SubmissionReviewFailure.None)]
    [InlineData(100, SubmissionStatus.Reviewed, SubmissionReviewFailure.None)]
    public void Review_EnforcesMarkingRules(
        int? marks,
        SubmissionStatus status,
        SubmissionReviewFailure expected)
    {
        var decimalMarks = marks is null ? null : (decimal?)marks.Value;
        Assert.Equal(expected, SubmissionRules.GetReviewFailure(Assignment(), decimalMarks, status));
    }

    [Fact]
    public void Resubmission_ResetsReviewStateButPreservesFeedback()
    {
        var submission = Submission();
        submission.Status = SubmissionStatus.Returned;
        submission.Marks = 65;
        submission.Feedback = "Please revise section two.";
        submission.ReviewedAt = Now.AddHours(-1);

        SubmissionRules.MarkAsResubmitted(submission, Now);

        Assert.Equal(SubmissionStatus.Submitted, submission.Status);
        Assert.Null(submission.Marks);
        Assert.Null(submission.ReviewedAt);
        Assert.Equal(Now, submission.UpdatedAt);
        Assert.Equal("Please revise section two.", submission.Feedback);
    }

    [Theory]
    [InlineData(SubmissionStatus.Reviewed, true)]
    [InlineData(SubmissionStatus.Returned, true)]
    [InlineData(SubmissionStatus.Submitted, false)]
    public void Review_UpdatesStatusFeedbackAndTimestamp(
        SubmissionStatus status,
        bool expectsReviewedAt)
    {
        var submission = Submission();

        SubmissionRules.ApplyReview(submission, 85, "  Strong answer.  ", status, Now);

        Assert.Equal(status, submission.Status);
        Assert.Equal(85, submission.Marks);
        Assert.Equal("Strong answer.", submission.Feedback);
        Assert.Equal(Now, submission.UpdatedAt);
        Assert.Equal(expectsReviewedAt ? Now : null, submission.ReviewedAt);
    }

    private static User Student() => new()
    {
        Id = "student-1",
        FullName = "Test Student",
        Email = "student@example.com",
        PasswordHash = "unused",
        Role = UserRole.Student,
        CourseId = "course-1",
        IsActive = true
    };

    private static Assignment Assignment() => new()
    {
        Id = "assignment-1",
        Title = "Test assignment",
        CourseId = "course-1",
        SubjectId = "subject-1",
        TeacherId = "teacher-1",
        Deadline = Now.AddDays(1),
        MaximumMarks = 100,
        Status = AssignmentStatus.Published
    };

    private static Submission Submission() => new()
    {
        Id = "submission-1",
        AssignmentId = "assignment-1",
        StudentId = "student-1",
        Answer = "Initial answer",
        Status = SubmissionStatus.Submitted
    };
}
