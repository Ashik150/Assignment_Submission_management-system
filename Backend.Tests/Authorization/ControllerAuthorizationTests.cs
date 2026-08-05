using System.Reflection;
using Backend.Controllers;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Tests.Authorization;

public sealed class ControllerAuthorizationTests
{
    public static TheoryData<Type, UserRole> RoleProtectedControllers => new()
    {
        { typeof(AdminCoursesController), UserRole.Admin },
        { typeof(AdminOverviewController), UserRole.Admin },
        { typeof(AdminSubjectsController), UserRole.Admin },
        { typeof(AdminUsersController), UserRole.Admin },
        { typeof(TeacherAssignmentsController), UserRole.Teacher },
        { typeof(TeacherSubmissionsController), UserRole.Teacher },
        { typeof(StudentAssignmentsController), UserRole.Student },
        { typeof(StudentSubmissionsController), UserRole.Student }
    };

    [Theory]
    [MemberData(nameof(RoleProtectedControllers))]
    public void RoleController_RequiresItsExpectedRole(Type controllerType, UserRole role)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(role.ToString(), authorize.Roles);
    }

    [Fact]
    public void SubmissionFileController_RequiresAuthenticatedUser()
    {
        var authorize = typeof(SubmissionFilesController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Null(authorize.Roles);
    }

    [Fact]
    public void LoginEndpoint_IsExplicitlyAnonymous()
    {
        var login = typeof(AuthController).GetMethod(nameof(AuthController.Login));

        Assert.NotNull(login);
        Assert.NotNull(login.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
