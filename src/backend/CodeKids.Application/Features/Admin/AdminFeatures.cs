using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed record TeacherCourseRateDto(
    Guid CourseId,
    string CourseName,
    int? CourseGrade,
    decimal? SessionAmount,
    decimal? MonthlySalary);

public sealed record TeacherCourseRateInput(
    Guid CourseId,
    decimal? SessionAmount = null,
    decimal? MonthlySalary = null);

public sealed record ManagedUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    Guid? ParentId,
    int? Grade,
    int TotalXp,
    string MobilePhone,
    string? WorkShift,
    IReadOnlyList<int> Stages,
    string? ContractType = null,
    decimal? PrimaryAmount = null,
    decimal? PrepAmount = null,
    decimal? SecondaryAmount = null,
    IReadOnlyList<TeacherCourseRateDto>? CourseRates = null);

public sealed record CreateManagedUserRequest(
    string? Email,
    string DisplayName,
    string Password,
    string Role,
    Guid? ParentId,
    int? Grade = null,
    string? MobilePhone = null,
    string? WorkShift = null,
    IReadOnlyList<int>? Stages = null,
    string? ContractType = null,
    decimal? PrimaryAmount = null,
    decimal? PrepAmount = null,
    decimal? SecondaryAmount = null,
    IReadOnlyList<TeacherCourseRateInput>? CourseRates = null);

public sealed record CreateManagedUserCommand(
    Guid AdminUserId,
    string? Email,
    string DisplayName,
    string Password,
    string Role,
    Guid? ParentId,
    int? Grade = null,
    string? MobilePhone = null,
    string? WorkShift = null,
    IReadOnlyList<int>? Stages = null,
    string? ContractType = null,
    decimal? PrimaryAmount = null,
    decimal? PrepAmount = null,
    decimal? SecondaryAmount = null,
    IReadOnlyList<TeacherCourseRateInput>? CourseRates = null) : ICommand<ManagedUserDto>;

public sealed record ListManagedUsersQuery(string? Role = null) : IQuery<IReadOnlyList<ManagedUserDto>>;

public sealed record CreateCourseRequest(
    string Title,
    string Theme,
    string Description,
    int? AgeMin,
    int? AgeMax,
    string? Term,
    IReadOnlyList<int>? Grades,
    int? SortOrder);

public sealed record CreateCourseCommand(
    string Title,
    string Theme,
    string Description,
    int? AgeMin,
    int? AgeMax,
    string? Term,
    IReadOnlyList<int>? Grades,
    int? SortOrder) : ICommand<IReadOnlyList<CourseSummaryDto>>;

public sealed record CourseSummaryDto(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string? Term,
    int? Grade,
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

        var email = NormalizeEmail(command.Email);
        var mobile = RegisterCommandHandler.NormalizePhone(command.MobilePhone);
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(mobile))
        {
            throw new InvalidOperationException("Email or mobile is required.");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
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

        var grade = role == UserRole.Student
            ? CreateCourseCommandHandler.NormalizeGrade(command.Grade)
            : null;
        var workShift = ParseWorkShift(role, command.WorkShift);
        var stages = ParseTeacherStages(role, command.Stages);
        var contract = ParseTeacherContract(role, command);
        if (!string.IsNullOrWhiteSpace(mobile)
            && await dbContext.Users.AnyAsync(x => x.MobilePhone == mobile, cancellationToken))
        {
            throw new InvalidOperationException("An account with that mobile number already exists.");
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
            Grade = grade,
            AvatarId = defaultAvatar?.Id,
            MobilePhone = mobile,
            WorkShift = workShift,
            Stages = stages,
            ContractType = contract.ContractType,
            PrimaryAmount = contract.PrimaryAmount,
            PrepAmount = contract.PrepAmount,
            SecondaryAmount = contract.SecondaryAmount,
            TotalXp = 0
        };

        dbContext.Users.Add(user);
        await ReplaceCourseRatesAsync(dbContext, user.Id, role, command.CourseRates, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        _ = admin;
        return await LoadDtoAsync(dbContext, user.Id, cancellationToken);
    }

    internal static string NormalizeEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    internal static TeacherWorkShift? ParseWorkShift(UserRole role, string? workShift)
    {
        if (role != UserRole.Teacher)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(workShift))
        {
            return TeacherWorkShift.Both;
        }

        if (!Enum.TryParse<TeacherWorkShift>(workShift.Trim(), true, out var shift)
            || shift is not (TeacherWorkShift.Am or TeacherWorkShift.Pm or TeacherWorkShift.Both))
        {
            throw new InvalidOperationException("Teacher work shift must be Am, Pm, or Both.");
        }

        return shift;
    }

    internal static string ParseTeacherStages(UserRole role, IReadOnlyList<int>? stages)
    {
        if (role != UserRole.Teacher)
        {
            return string.Empty;
        }

        return GradeStageHelper.SerializeStages(stages);
    }

    internal static (
        TeacherContractType? ContractType,
        decimal? PrimaryAmount,
        decimal? PrepAmount,
        decimal? SecondaryAmount)
        ParseTeacherContract(
            UserRole role,
            string? contractType,
            decimal? primaryAmount,
            decimal? prepAmount,
            decimal? secondaryAmount)
    {
        if (role != UserRole.Teacher)
        {
            return (null, null, null, null);
        }

        TeacherContractType parsedType;
        if (string.IsNullOrWhiteSpace(contractType))
        {
            parsedType = TeacherContractType.Session;
        }
        else if (!Enum.TryParse(contractType.Trim(), true, out parsedType)
                 || parsedType is not (TeacherContractType.Session or TeacherContractType.Monthly))
        {
            throw new InvalidOperationException("Teacher contract type must be Session or Monthly.");
        }

        return (
            parsedType,
            NormalizeMoney(primaryAmount),
            NormalizeMoney(prepAmount),
            NormalizeMoney(secondaryAmount));
    }

    private static (
        TeacherContractType? ContractType,
        decimal? PrimaryAmount,
        decimal? PrepAmount,
        decimal? SecondaryAmount)
        ParseTeacherContract(UserRole role, CreateManagedUserCommand command) =>
        ParseTeacherContract(
            role,
            command.ContractType,
            command.PrimaryAmount,
            command.PrepAmount,
            command.SecondaryAmount);

    internal static decimal? NormalizeMoney(decimal? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value < 0)
        {
            throw new InvalidOperationException("Payment amounts cannot be negative.");
        }

        return Math.Round(value.Value, 2, MidpointRounding.AwayFromZero);
    }

    internal static async Task ReplaceCourseRatesAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        UserRole role,
        IReadOnlyList<TeacherCourseRateInput>? rates,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.TeacherCourseRates
            .Where(x => x.TeacherId == teacherId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            dbContext.TeacherCourseRates.RemoveRange(existing);
        }

        if (role != UserRole.Teacher || rates is null || rates.Count == 0)
        {
            return;
        }

        var courseIds = rates.Select(x => x.CourseId).Distinct().ToList();
        if (courseIds.Count != rates.Count)
        {
            throw new InvalidOperationException("Each subject rate must use a unique course.");
        }

        var matched = await dbContext.Courses
            .AsNoTracking()
            .Where(x => courseIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        if (matched.Count != courseIds.Count)
        {
            throw new InvalidOperationException("One or more courses were not found for teacher rates.");
        }

        foreach (var rate in rates)
        {
            var sessionAmount = NormalizeMoney(rate.SessionAmount);
            var monthlySalary = NormalizeMoney(rate.MonthlySalary);
            if (sessionAmount is null && monthlySalary is null)
            {
                throw new InvalidOperationException("Each subject rate needs a session amount or monthly salary.");
            }

            dbContext.TeacherCourseRates.Add(new TeacherCourseRate
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                CourseId = rate.CourseId,
                SessionAmount = sessionAmount,
                MonthlySalary = monthlySalary,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
    }

    internal static async Task<ManagedUserDto> LoadDtoAsync(
        IAppDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.CourseRates)
            .ThenInclude(x => x.Course)
            .FirstAsync(x => x.Id == userId, cancellationToken);
        return ToDto(user);
    }

    internal static ManagedUserDto ToDto(User user)
    {
        var rates = user.Role == UserRole.Teacher
            ? (user.CourseRates ?? [])
                .OrderBy(x => x.Course?.Grade ?? 999)
                .ThenBy(x => x.Course?.Title)
                .Select(x => new TeacherCourseRateDto(
                    x.CourseId,
                    x.Course?.Title ?? string.Empty,
                    x.Course?.Grade,
                    x.SessionAmount,
                    x.MonthlySalary))
                .ToList()
            : [];

        return new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString(),
            user.ParentId,
            user.Grade,
            user.TotalXp,
            user.MobilePhone,
            user.WorkShift?.ToString(),
            user.Role == UserRole.Teacher
                ? GradeStageHelper.ParseStages(user.Stages)
                : Array.Empty<int>(),
            user.ContractType?.ToString(),
            user.PrimaryAmount,
            user.PrepAmount,
            user.SecondaryAmount,
            rates);
    }
}

public sealed class ListManagedUsersQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListManagedUsersQuery, IReadOnlyList<ManagedUserDto>>
{
    public async Task<IReadOnlyList<ManagedUserDto>> Handle(ListManagedUsersQuery query, CancellationToken cancellationToken)
    {
        var users = dbContext.Users
            .AsNoTracking()
            .Include(x => x.CourseRates)
            .ThenInclude(x => x.Course)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Role) &&
            Enum.TryParse<UserRole>(query.Role, true, out var role))
        {
            users = users.Where(x => x.Role == role);
        }

        return (await users
            .OrderBy(x => x.Role)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken))
            .Select(CreateManagedUserCommandHandler.ToDto)
            .ToList();
    }
}

public sealed class CreateCourseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateCourseCommand, IReadOnlyList<CourseSummaryDto>>
{
    public async Task<IReadOnlyList<CourseSummaryDto>> Handle(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Course title is required.");
        }

        var term = ParseTerm(command.Term);
        var grades = NormalizeGrades(command.Grades);
        var theme = string.IsNullOrWhiteSpace(command.Theme) ? "General" : command.Theme.Trim();
        var description = (command.Description ?? string.Empty).Trim();
        var ageMin = command.AgeMin is null or <= 0 ? 8 : command.AgeMin.Value;
        var ageMax = command.AgeMax is null or <= 0 ? 12 : command.AgeMax.Value;
        var sortOrder = command.SortOrder ?? 0;

        var courses = grades.Select(grade => new Course
        {
            Id = Guid.NewGuid(),
            Title = title,
            Theme = theme,
            Description = description,
            AgeMin = ageMin,
            AgeMax = ageMax,
            Term = term,
            Grade = grade,
            SortOrder = sortOrder
        }).ToList();

        dbContext.Courses.AddRange(courses);
        await dbContext.SaveChangesAsync(cancellationToken);

        return courses.Select(ToSummary).ToList();
    }

    internal static CourseTerm? ParseTerm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<CourseTerm>(value.Trim(), true, out var term))
        {
            throw new InvalidOperationException("Term must be FirstTerm, SecondTerm, or FullYear.");
        }

        return term;
    }

    /// <summary>
    /// Empty/null grades → one course for all grades (null).
    /// Otherwise one course per distinct grade (KG1=-1, KG2=0, or 1–12).
    /// </summary>
    internal static IReadOnlyList<int?> NormalizeGrades(IReadOnlyList<int>? grades)
    {
        if (grades is null || grades.Count == 0)
        {
            return [null];
        }

        return grades
            .Select(g => NormalizeGrade(g))
            .Distinct()
            .OrderBy(g => g ?? 999)
            .ToList();
    }

    /// <summary>KG1 = -1, KG2 = 0, grades 1–12; null means all grades.</summary>
    internal static int? NormalizeGrade(int? grade)
    {
        if (grade is null)
        {
            return null;
        }

        if (grade is < -1 or > 12)
        {
            throw new InvalidOperationException("Grade must be KG1, KG2, or between 1 and 12.");
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
            course.Term?.ToString(),
            course.Grade,
            course.SortOrder);
}

public sealed record UpdateManagedUserRequest(
    string? Email,
    string DisplayName,
    string Role,
    Guid? ParentId,
    string? Password,
    int? Grade = null,
    string? MobilePhone = null,
    string? WorkShift = null,
    IReadOnlyList<int>? Stages = null,
    string? ContractType = null,
    decimal? PrimaryAmount = null,
    decimal? PrepAmount = null,
    decimal? SecondaryAmount = null,
    IReadOnlyList<TeacherCourseRateInput>? CourseRates = null);

public sealed record UpdateManagedUserCommand(
    Guid AdminUserId,
    Guid UserId,
    string? Email,
    string DisplayName,
    string Role,
    Guid? ParentId,
    string? Password,
    int? Grade = null,
    string? MobilePhone = null,
    string? WorkShift = null,
    IReadOnlyList<int>? Stages = null,
    string? ContractType = null,
    decimal? PrimaryAmount = null,
    decimal? PrepAmount = null,
    decimal? SecondaryAmount = null,
    IReadOnlyList<TeacherCourseRateInput>? CourseRates = null) : ICommand<ManagedUserDto>;

public sealed record DeleteManagedUserCommand(Guid AdminUserId, Guid UserId) : ICommand<bool>;

public sealed record UpdateCourseRequest(
    string Title,
    string Theme,
    string Description,
    int? AgeMin,
    int? AgeMax,
    string? Term,
    int? Grade,
    int? SortOrder);

public sealed record UpdateCourseCommand(
    Guid CourseId,
    string Title,
    string Theme,
    string Description,
    int? AgeMin,
    int? AgeMax,
    string? Term,
    int? Grade,
    int? SortOrder) : ICommand<CourseSummaryDto>;

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

        var email = CreateManagedUserCommandHandler.NormalizeEmail(command.Email);
        var mobile = RegisterCommandHandler.NormalizePhone(command.MobilePhone);
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(mobile))
        {
            throw new InvalidOperationException("Email or mobile is required.");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && await dbContext.Users.AnyAsync(x => x.Email == email && x.Id != user.Id, cancellationToken))
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
        user.Grade = role == UserRole.Student
            ? CreateCourseCommandHandler.NormalizeGrade(command.Grade)
            : null;
        if (!string.IsNullOrWhiteSpace(mobile)
            && await dbContext.Users.AnyAsync(x => x.MobilePhone == mobile && x.Id != user.Id, cancellationToken))
        {
            throw new InvalidOperationException("An account with that mobile number already exists.");
        }

        user.MobilePhone = mobile;
        user.WorkShift = CreateManagedUserCommandHandler.ParseWorkShift(role, command.WorkShift);
        user.Stages = CreateManagedUserCommandHandler.ParseTeacherStages(role, command.Stages);
        var contract = CreateManagedUserCommandHandler.ParseTeacherContract(
            role,
            command.ContractType,
            command.PrimaryAmount,
            command.PrepAmount,
            command.SecondaryAmount);
        user.ContractType = contract.ContractType;
        user.PrimaryAmount = contract.PrimaryAmount;
        user.PrepAmount = contract.PrepAmount;
        user.SecondaryAmount = contract.SecondaryAmount;
        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            user.PasswordHash = passwordHasher.Hash(command.Password);
        }

        await CreateManagedUserCommandHandler.ReplaceCourseRatesAsync(
            dbContext,
            user.Id,
            role,
            command.CourseRates,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await CreateManagedUserCommandHandler.LoadDtoAsync(dbContext, user.Id, cancellationToken);
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

        var teacherLinks = await dbContext.ClassroomCourses
            .Where(x => x.TeacherId == user.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomCourses.RemoveRange(teacherLinks);

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
        course.AgeMin = command.AgeMin is null or <= 0 ? 8 : command.AgeMin.Value;
        course.AgeMax = command.AgeMax is null or <= 0 ? 12 : command.AgeMax.Value;
        course.Term = CreateCourseCommandHandler.ParseTerm(command.Term);
        course.Grade = CreateCourseCommandHandler.NormalizeGrade(command.Grade);
        course.SortOrder = command.SortOrder ?? 0;

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

        var courseLinks = await dbContext.ClassroomCourses
            .Where(x => x.CourseId == course.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomCourses.RemoveRange(courseLinks);

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
