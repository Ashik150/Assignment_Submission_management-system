using Backend.Models;
using Backend.Rules;

namespace Backend.Tests.Rules;

public sealed class SubmissionAuthorizationRulesTests
{
    private readonly Submission submission = new()
    {
        Id = "submission-1",
        AssignmentId = "assignment-1",
        StudentId = "student-1"
    };

    [Fact]
    public void Administrator_CanDownloadAnySubmissionPdf()
    {
        Assert.True(SubmissionAuthorizationRules.CanDownloadPdf(
            UserRole.Admin,
            "admin-1",
            submission));
    }

    [Fact]
    public void Student_CanDownloadOwnSubmissionPdf()
    {
        Assert.True(SubmissionAuthorizationRules.CanDownloadPdf(
            UserRole.Student,
            "student-1",
            submission));
    }

    [Fact]
    public void Student_CannotDownloadAnotherStudentsPdf()
    {
        Assert.False(SubmissionAuthorizationRules.CanDownloadPdf(
            UserRole.Student,
            "student-2",
            submission));
    }

    [Fact]
    public void AssignedTeacher_CanDownloadSubmissionPdf()
    {
        Assert.True(SubmissionAuthorizationRules.CanDownloadPdf(
            UserRole.Teacher,
            "teacher-1",
            submission,
            Assignment("assignment-1", "teacher-1")));
    }

    [Theory]
    [InlineData("assignment-2", "teacher-1")]
    [InlineData("assignment-1", "teacher-2")]
    public void UnrelatedTeacher_CannotDownloadSubmissionPdf(
        string assignmentId,
        string teacherId)
    {
        Assert.False(SubmissionAuthorizationRules.CanDownloadPdf(
            UserRole.Teacher,
            "teacher-1",
            submission,
            Assignment(assignmentId, teacherId)));
    }

    [Fact]
    public void TeacherWithoutAssignment_CannotDownloadSubmissionPdf()
    {
        Assert.False(SubmissionAuthorizationRules.CanDownloadPdf(
            UserRole.Teacher,
            "teacher-1",
            submission));
    }

    private static Assignment Assignment(string id, string teacherId) => new()
    {
        Id = id,
        Title = "Test assignment",
        CourseId = "course-1",
        SubjectId = "subject-1",
        TeacherId = teacherId,
        Deadline = DateTime.UtcNow.AddDays(1),
        MaximumMarks = 100,
        Status = AssignmentStatus.Published
    };
}
