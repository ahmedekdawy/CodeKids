using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class CreateManagedUserCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher) : ICommandHandler<CreateManagedUserCommand, ManagedUserDto>
{
    public async Task<ManagedUserDto> Handle(CreateManagedUserCommand command, CancellationToken cancellationToken)
    {
        if (command.Role != "SuperAdmin")
        {
            var admin = await dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == command.AdminUserId && x.Role == UserRole.SuperAdmin, cancellationToken)
                ?? throw new InvalidOperationException("Super Admin account not found.");
        }

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
        var schoolType = ParseSchoolType(role, command.SchoolType);
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
            SchoolType = schoolType,
            AvatarId = defaultAvatar?.Id,
            MobilePhone = mobile,
            WorkShift = workShift,
            Stages = stages,
            ContractType = contract.ContractType,
            PrimaryAmount = contract.PrimaryAmount,
            PrepAmount = contract.PrepAmount,
            SecondaryAmount = contract.SecondaryAmount,
            MonthlySalary = CreateManagedUserCommandHandler.NormalizeMoney(command.MonthlySalary),
            TotalXp = 0
        };

        dbContext.Users.Add(user);
        await ReplaceCourseRatesAsync(dbContext, user.Id, role, command.CourseRates, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        //_ = admin;
        return await LoadDtoAsync(dbContext, user.Id, cancellationToken);
    }

    internal static string NormalizeEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    internal static SchoolType? ParseSchoolType(UserRole role, string? schoolType)
    {
        if (role != UserRole.Student)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(schoolType))
        {
            return null;
        }

        if (!Enum.TryParse<SchoolType>(schoolType.Trim(), true, out var parsed)
            || parsed is not (SchoolType.Arabic or SchoolType.Language))
        {
            throw new InvalidOperationException("Student school type must be Arabic or Language.");
        }

        return parsed;
    }

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
            user.SchoolType?.ToString(),
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
            user.MonthlySalary,
            user.IsActive,
            rates);
    }
}
