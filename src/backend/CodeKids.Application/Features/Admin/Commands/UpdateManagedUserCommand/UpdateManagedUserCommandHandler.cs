using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class UpdateManagedUserCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher) : ICommandHandler<UpdateManagedUserCommand, ManagedUserDto>
{
    public async Task<ManagedUserDto> Handle(UpdateManagedUserCommand command, CancellationToken cancellationToken)
    {
        _ = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.AdminUserId && (x.Role == UserRole.SuperAdmin || x.IsTenantOwner), cancellationToken)
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

        if (user.IsTenantOwner && role != user.Role)
        {
            throw new InvalidOperationException("The account owner's role cannot be changed.");
        }

        if (user.Role == UserRole.SuperAdmin && role != UserRole.SuperAdmin)
        {
            var adminCount = await dbContext.Users.CountAsync(x => x.Role == UserRole.SuperAdmin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot demote the last Super Admin.");
            }
        }

        var previousRole = user.Role;
        user.Email = email;
        user.DisplayName = command.DisplayName.Trim();
        user.Role = role;
        user.EnsureRoleProfile();
        // A user whose role changed away from student keeps the row (XP, avatar) but loses these.
        if (role == UserRole.Student || user.StudentProfile is not null)
        {
            var student = user.EnsureStudentProfile();
            student.ParentId = role == UserRole.Student ? command.ParentId : null;
            student.Grade = role == UserRole.Student
                ? CreateCourseCommandHandler.NormalizeGrade(command.Grade)
                : null;
            student.SchoolType = CreateManagedUserCommandHandler.ParseSchoolType(role, command.SchoolType);
            if (role == UserRole.Student)
            {
                student.Gender = CreateManagedUserCommandHandler.ParseGender(command.Gender);
                student.TimeZone = Tenants.TimeZoneIds.Normalize(command.TimeZone);
                (student.SessionMinutes, student.SessionsPerWeek) =
                    CreateManagedUserCommandHandler.ValidateStudentSessions(command.SessionMinutes, command.SessionsPerWeek);
            }
        }
        if (!string.IsNullOrWhiteSpace(mobile)
            && await dbContext.Users.AnyAsync(x => x.MobilePhone == mobile && x.Id != user.Id, cancellationToken))
        {
            throw new InvalidOperationException("An account with that mobile number already exists.");
        }

        user.MobilePhone = mobile;

        // Moving a user to another base role drops roles that no longer apply and grants the new default.
        var assignments = new List<UserInRole>();
        if (previousRole != role)
        {
            assignments = await dbContext.UserInRoles
                .Where(x => x.UserId == user.Id)
                .ToListAsync(cancellationToken);
            if (assignments.Count > 0)
            {
                var roleIds = assignments.Select(x => x.RoleId).ToList();
                var assignedRoles = await dbContext.Roles
                    .Where(x => roleIds.Contains(x.Id))
                    .ToListAsync(cancellationToken);
                dbContext.UserInRoles.RemoveRange(assignments.Where(assignment =>
                    assignedRoles.All(x => x.Id != assignment.RoleId || x.BaseRole != role)));
            }

            var defaultRole = await dbContext.Roles
                .FirstOrDefaultAsync(x => x.IsSystem && x.BaseRole == role, cancellationToken);
            if (defaultRole is not null
                && !assignments.Any(x => x.RoleId == defaultRole.Id))
            {
                dbContext.UserInRoles.Add(new UserInRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = defaultRole.Id
                });
            }
        }

        // The Parse* helpers return empty values for non-teachers, clearing a former teacher's settings.
        if (role == UserRole.Teacher || user.TeacherProfile is not null)
        {
            var teacher = user.EnsureTeacherProfile();
            teacher.WorkShift = CreateManagedUserCommandHandler.ParseWorkShift(role, command.WorkShift);
            teacher.Stages = CreateManagedUserCommandHandler.ParseTeacherStages(role, command.Stages);
            var contract = CreateManagedUserCommandHandler.ParseTeacherContract(
                role,
                command.ContractType,
                command.PrimaryAmount,
                command.PrepAmount,
                command.SecondaryAmount);
            teacher.ContractType = contract.ContractType;
            teacher.PrimaryAmount = contract.PrimaryAmount;
            teacher.PrepAmount = contract.PrepAmount;
            teacher.SecondaryAmount = contract.SecondaryAmount;
            teacher.MonthlySalary = CreateManagedUserCommandHandler.NormalizeMoney(command.MonthlySalary);
            if (role == UserRole.Teacher)
            {
                teacher.Gender = CreateManagedUserCommandHandler.ParseGender(command.Gender);
                teacher.TimeZone = Tenants.TimeZoneIds.Normalize(command.TimeZone);
            }
        }

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
