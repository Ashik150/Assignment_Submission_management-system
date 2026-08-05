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
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/users")]
public sealed class AdminUsersController(
    MongoDbContext database,
    PasswordHasher passwordHasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(
        [FromQuery] UserRole? role,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = Builders<User>.Filter.In(
            user => user.Role,
            new[] { UserRole.Teacher, UserRole.Student });

        if (role is not null)
        {
            if (!IsManagedRole(role.Value))
            {
                return BadRequestProblem("Only Teacher and Student roles can be managed here.");
            }

            filter &= Builders<User>.Filter.Eq(user => user.Role, role.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = System.Text.RegularExpressions.Regex.Escape(search.Trim());
            var searchFilter = Builders<User>.Filter.Regex(user => user.FullName, new BsonRegularExpression(escaped, "i")) |
                Builders<User>.Filter.Regex(user => user.Email, new BsonRegularExpression(escaped, "i"));
            filter &= searchFilter;
        }

        var users = await database.Users.Find(filter)
            .SortBy(user => user.FullName)
            .ToListAsync(cancellationToken);

        return Ok(users.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequestProblem("The user ID is invalid.");
        }

        var user = await database.Users.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null || !IsManagedRole(user.Role)
            ? NotFoundProblem("User not found.")
            : Ok(ToResponse(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsManagedRole(request.Role))
        {
            return BadRequestProblem("Only Teacher and Student users can be created.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = request.IsActive
        };

        try
        {
            await database.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ConflictProblem("A user with this email address already exists.");
        }

        var response = ToResponse(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> Update(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequestProblem("The user ID is invalid.");
        }

        if (!IsManagedRole(request.Role))
        {
            return BadRequestProblem("Only Teacher and Student roles can be assigned.");
        }

        var user = await database.Users.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null || !IsManagedRole(user.Role))
        {
            return NotFoundProblem("User not found.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = passwordHasher.Hash(request.Password);
        }

        try
        {
            await database.Users.ReplaceOneAsync(
                candidate => candidate.Id == id,
                user,
                cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ConflictProblem("A user with this email address already exists.");
        }

        return Ok(ToResponse(user));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequestProblem("The user ID is invalid.");
        }

        var user = await database.Users.Find(candidate => candidate.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null || !IsManagedRole(user.Role))
        {
            return NotFoundProblem("User not found.");
        }

        var isReferenced = user.Role == UserRole.Teacher
            ? await database.Subjects.Find(subject => subject.TeacherId == id).AnyAsync(cancellationToken) ||
              await database.Assignments.Find(assignment => assignment.TeacherId == id).AnyAsync(cancellationToken)
            : await database.Submissions.Find(submission => submission.StudentId == id).AnyAsync(cancellationToken);

        if (isReferenced)
        {
            return ConflictProblem(
                "This user is referenced by academic records. Deactivate the account instead of deleting it.");
        }

        await database.Users.DeleteOneAsync(candidate => candidate.Id == id, cancellationToken);
        return NoContent();
    }

    private static bool IsManagedRole(UserRole role) =>
        role is UserRole.Teacher or UserRole.Student;

    private static UserResponse ToResponse(User user) => new(
        user.Id!,
        user.FullName,
        user.Email,
        user.Role,
        user.IsActive,
        user.CreatedAt,
        user.UpdatedAt);

    private ObjectResult BadRequestProblem(string detail) =>
        Problem(title: "Invalid user request", detail: detail, statusCode: StatusCodes.Status400BadRequest);

    private ObjectResult NotFoundProblem(string detail) =>
        Problem(title: "User not found", detail: detail, statusCode: StatusCodes.Status404NotFound);

    private ObjectResult ConflictProblem(string detail) =>
        Problem(title: "User conflict", detail: detail, statusCode: StatusCodes.Status409Conflict);
}
