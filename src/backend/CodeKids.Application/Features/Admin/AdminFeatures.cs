using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed record ManagedUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    Guid? ParentId,
    int TotalXp,
    string MobilePhone);

public sealed record CreateManagedUserRequest(
    string Email,
    string DisplayName,
    string Password,
    string Role,
    Guid? ParentId,
    string? MobilePhone = null);

public sealed record CreateManagedUserCommand(
    Guid AdminUserId,
    string Email,
    string DisplayName,
    string Password,
    string Role,
    Guid? ParentId,
    string? MobilePhone = null) : ICommand<ManagedUserDto>;

public sealed record ListManagedUsersQuery(string? Role = null) : IQuery<IReadOnlyList<ManagedUserDto>>;

public sealed record CreateCourseRequest(
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string Term,
    int Grade,
    int SortOrder);

public sealed record CreateCourseCommand(
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string Term,
    int Grade,
    int SortOrder) : ICommand<CourseSummaryDto>;

public sealed record CourseSummaryDto(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string Term,
    int Grade,
    int SortOrder);

public sealed class CreateManagedUserCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher) : ICommandHandler<CreateManagedUserCommand, ManagedUserDto>
{
    public async Task<ManagedUserDto> Handle(CreateManagedUserCommand command, CancellationToken cancellationToken)
    {
        var admin = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.AdminUserId && x.Role == UserRole.SuperAdmin, cancellationToken)
            ?? throw new InvalidOperationException("Super Admin account not found.");

        if (!Enum.TryParse<UserRole>(command.Role, true, out var role) ||
            role is not (UserRole.Teacher or UserRole.Student or UserRole.Parent or UserRole.SuperAdmin))
        {
            throw new InvalidOperationException("Role must be Teacher, Student, Parent, or SuperAdmin.");
        }

        var email = command.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("An account with that email already exists.");
        }

        if (role == UserRole.Student && command.ParentId is Guid parentId)
        {
            var parentExists = await dbContext.Users.AnyAsync(
                x => x.Id == parentId && x.Role == UserRole.Parent, cancellationToken);
            if (!parentExists)
            {
                throw new InvalidOperationException("Parent account was not found.");
            }
        }

        var defaultAvatar = role == UserRole.Student
            ? await dbContext.Avatars.OrderBy(x => x.UnlockXp).FirstOrDefaultAsync(cancellationToken)
            : null;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = command.DisplayName.Trim(),
            PasswordHash = passwordHasher.Hash(command.Password),
            Role = role,
            ParentId = role == UserRole.Student ? command.ParentId : null,
            AvatarId = defaultAvatar?.Id,
            MobilePhone = NormalizePhone(command.MobilePhone),
            TotalXp = 0
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        _ = admin;
        return ToDto(user);
    }

    internal static string NormalizePhone(string? phone) =>
        (phone ?? string.Empty).Trim();

    internal static ManagedUserDto ToDto(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role.ToString(), user.ParentId, user.TotalXp, user.MobilePhone);
}

public sealed class ListManagedUsersQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListManagedUsersQuery, IReadOnlyList<ManagedUserDto>>
{
    public async Task<IReadOnlyList<ManagedUserDto>> Handle(ListManagedUsersQuery query, CancellationToken cancellationToken)
    {
        var users = dbContext.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Role) &&
            Enum.TryParse<UserRole>(query.Role, true, out var role))
        {
            users = users.Where(x => x.Role == role);
        }

        return await users
            .OrderBy(x => x.Role)
            .ThenBy(x => x.DisplayName)
            .Select(x => new ManagedUserDto(
                x.Id, x.Email, x.DisplayName, x.Role.ToString(), x.ParentId, x.TotalXp, x.MobilePhone))
            .ToListAsync(cancellationToken);
    }
}

public sealed class CreateCourseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateCourseCommand, CourseSummaryDto>
{
    public async Task<CourseSummaryDto> Handle(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Course title is required.");
        }

        var term = ParseTerm(command.Term);
        var grade = NormalizeGrade(command.Grade);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = title,
            Theme = string.IsNullOrWhiteSpace(command.Theme) ? "General" : command.Theme.Trim(),
            Description = (command.Description ?? string.Empty).Trim(),
            AgeMin = command.AgeMin <= 0 ? 8 : command.AgeMin,
            AgeMax = command.AgeMax <= 0 ? 12 : command.AgeMax,
            Term = term,
            Grade = grade,
            SortOrder = command.SortOrder
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToSummary(course);
    }

    internal static CourseTerm ParseTerm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CourseTerm.FullYear;
        }

        if (!Enum.TryParse<CourseTerm>(value.Trim(), true, out var term))
        {
            throw new InvalidOperationException("Term must be FirstTerm, SecondTerm, or FullYear.");
        }

        return term;
    }

    internal static int NormalizeGrade(int grade)
    {
        if (grade is < 1 or > 12)
        {
            throw new InvalidOperationException("Grade must be between 1 and 12.");
        }

        return grade;
    }

    internal static CourseSummaryDto ToSummary(Course course) =>
        new(
            course.Id,
            course.Title,
            course.Theme,
            course.Description,
            course.AgeMin,
            course.AgeMax,
            course.Term.ToString(),
            course.Grade,
            course.SortOrder);
}

public sealed record UpdateManagedUserRequest(
    string Email,
    string DisplayName,
    string Role,
    Guid? ParentId,
    string? Password,
    string? MobilePhone = null);

public sealed record UpdateManagedUserCommand(
    Guid AdminUserId,
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    Guid? ParentId,
    string? Password,
    string? MobilePhone = null) : ICommand<ManagedUserDto>;

public sealed record DeleteManagedUserCommand(Guid AdminUserId, Guid UserId) : ICommand<bool>;

public sealed record UpdateCourseRequest(
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string Term,
    int Grade,
    int SortOrder);

public sealed record UpdateCourseCommand(
    Guid CourseId,
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string Term,
    int Grade,
    int SortOrder) : ICommand<CourseSummaryDto>;

public sealed record DeleteCourseCommand(Guid CourseId) : ICommand<bool>;

public sealed class UpdateManagedUserCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher) : ICommandHandler<UpdateManagedUserCommand, ManagedUserDto>
{
    public async Task<ManagedUserDto> Handle(UpdateManagedUserCommand command, CancellationToken cancellationToken)
    {
        _ = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.AdminUserId && x.Role == UserRole.SuperAdmin, cancellationToken)
            ?? throw new InvalidOperationException("Super Admin account not found.");

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (!Enum.TryParse<UserRole>(command.Role, true, out var role) ||
            role is not (UserRole.Teacher or UserRole.Student or UserRole.Parent or UserRole.SuperAdmin))
        {
            throw new InvalidOperationException("Role must be Teacher, Student, Parent, or SuperAdmin.");
        }

        var email = command.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email == email && x.Id != user.Id, cancellationToken))
        {
            throw new InvalidOperationException("An account with that email already exists.");
        }

        if (role == UserRole.Student && command.ParentId is Guid parentId)
        {
            var parentExists = await dbContext.Users.AnyAsync(
                x => x.Id == parentId && x.Role == UserRole.Parent, cancellationToken);
            if (!parentExists)
            {
                throw new InvalidOperationException("Parent account was not found.");
            }
        }

        if (user.Role == UserRole.SuperAdmin && role != UserRole.SuperAdmin)
        {
            var adminCount = await dbContext.Users.CountAsync(x => x.Role == UserRole.SuperAdmin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot demote the last Super Admin.");
            }
        }

        user.Email = email;
        user.DisplayName = command.DisplayName.Trim();
        user.Role = role;
        user.ParentId = role == UserRole.Student ? command.ParentId : null;
        user.MobilePhone = CreateManagedUserCommandHandler.NormalizePhone(command.MobilePhone);
        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            user.PasswordHash = passwordHasher.Hash(command.Password);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateManagedUserCommandHandler.ToDto(user);
    }
}

public sealed class DeleteManagedUserCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteManagedUserCommand, bool>
{
    public async Task<bool> Handle(DeleteManagedUserCommand command, CancellationToken cancellationToken)
    {
        if (command.AdminUserId == command.UserId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (user.Role == UserRole.SuperAdmin)
        {
            var adminCount = await dbContext.Users.CountAsync(x => x.Role == UserRole.SuperAdmin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot delete the last Super Admin.");
            }
        }

        var classroomsAsTeacher = await dbContext.Classrooms
            .Where(x => x.TeacherId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var classroom in classroomsAsTeacher)
        {
            classroom.TeacherId = null;
        }

        var memberships = await dbContext.ClassroomStudents
            .Where(x => x.StudentId == user.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomStudents.RemoveRange(memberships);

        var children = await dbContext.Users.Where(x => x.ParentId == user.Id).ToListAsync(cancellationToken);
        foreach (var child in children)
        {
            child.ParentId = null;
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class UpdateCourseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateCourseCommand, CourseSummaryDto>
{
    public async Task<CourseSummaryDto> Handle(UpdateCourseCommand command, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Course title is required.");
        }

        course.Title = title;
        course.Theme = string.IsNullOrWhiteSpace(command.Theme) ? "General" : command.Theme.Trim();
        course.Description = (command.Description ?? string.Empty).Trim();
        course.AgeMin = command.AgeMin <= 0 ? 8 : command.AgeMin;
        course.AgeMax = command.AgeMax <= 0 ? 12 : command.AgeMax;
        course.Term = CreateCourseCommandHandler.ParseTerm(command.Term);
        course.Grade = CreateCourseCommandHandler.NormalizeGrade(command.Grade);
        course.SortOrder = command.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateCourseCommandHandler.ToSummary(course);
    }
}

public sealed class DeleteCourseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteCourseCommand, bool>
{
    public async Task<bool> Handle(DeleteCourseCommand command, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var classrooms = await dbContext.Classrooms.Where(x => x.CourseId == course.Id).ToListAsync(cancellationToken);
        foreach (var classroom in classrooms)
        {
            classroom.CourseId = null;
        }

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
