using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record ClassroomStudentDto(Guid StudentId, string DisplayName, string Email);

public sealed record ClassroomDto(
    Guid Id,
    string Name,
    string Description,
    Guid? TeacherId,
    string? TeacherName,
    Guid? CourseId,
    string? CourseTitle,
    string WhatsAppGroupInviteUrl,
    string WhatsAppNotifyPhones,
    IReadOnlyList<ClassroomStudentDto> Students);

public sealed record CreateClassroomRequest(
    string Name,
    string? Description,
    Guid? TeacherId,
    Guid? CourseId,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones);

public sealed record AssignClassroomRequest(Guid? TeacherId, Guid? CourseId);

public sealed record AddClassroomStudentRequest(Guid StudentId);

public sealed record CreateClassroomCommand(
    string Name,
    string? Description,
    Guid? TeacherId,
    Guid? CourseId,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones) : ICommand<ClassroomDto>;

public sealed record UpdateClassroomAssignmentsCommand(
    Guid ClassroomId,
    Guid? TeacherId,
    Guid? CourseId) : ICommand<ClassroomDto>;

public sealed record AddStudentToClassroomCommand(Guid ClassroomId, Guid StudentId) : ICommand<ClassroomDto>;

public sealed record UpdateClassroomWhatsAppRequest(
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones);

public sealed record UpdateClassroomWhatsAppCommand(
    Guid ClassroomId,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones) : ICommand<ClassroomDto>;

public sealed record GetClassroomsQuery(Guid ViewerUserId, string ViewerRole) : IQuery<IReadOnlyList<ClassroomDto>>;

public sealed record GetClassroomByIdQuery(Guid ClassroomId) : IQuery<ClassroomDto?>;

public sealed class CreateClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateClassroomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(CreateClassroomCommand command, CancellationToken cancellationToken)
    {
        await ValidateTeacherAndCourse(dbContext, command.TeacherId, command.CourseId, cancellationToken);

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Description = (command.Description ?? string.Empty).Trim(),
            TeacherId = command.TeacherId,
            CourseId = command.CourseId,
            WhatsAppGroupInviteUrl = (command.WhatsAppGroupInviteUrl ?? string.Empty).Trim(),
            WhatsAppNotifyPhones = (command.WhatsAppNotifyPhones ?? string.Empty).Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (string.IsNullOrWhiteSpace(classroom.Name))
        {
            throw new InvalidOperationException("Classroom name is required.");
        }

        dbContext.Classrooms.Add(classroom);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }

    internal static async Task ValidateTeacherAndCourse(
        IAppDbContext dbContext,
        Guid? teacherId,
        Guid? courseId,
        CancellationToken cancellationToken)
    {
        if (teacherId is Guid tid)
        {
            var ok = await dbContext.Users.AnyAsync(x => x.Id == tid && x.Role == UserRole.Teacher, cancellationToken);
            if (!ok) throw new InvalidOperationException("Teacher not found.");
        }

        if (courseId is Guid cid)
        {
            var ok = await dbContext.Courses.AnyAsync(x => x.Id == cid, cancellationToken);
            if (!ok) throw new InvalidOperationException("Course not found.");
        }
    }

    internal static async Task<ClassroomDto?> LoadDto(IAppDbContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .Include(x => x.Students)
                .ThenInclude(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return classroom is null ? null : Map(classroom);
    }

    internal static ClassroomDto Map(Classroom classroom) =>
        new(
            classroom.Id,
            classroom.Name,
            classroom.Description,
            classroom.TeacherId,
            classroom.Teacher?.DisplayName,
            classroom.CourseId,
            classroom.Course?.Title,
            classroom.WhatsAppGroupInviteUrl,
            classroom.WhatsAppNotifyPhones,
            classroom.Students
                .Where(x => x.Student is not null)
                .Select(x => new ClassroomStudentDto(x.StudentId, x.Student!.DisplayName, x.Student.Email))
                .OrderBy(x => x.DisplayName)
                .ToList());
}

public sealed class UpdateClassroomAssignmentsCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateClassroomAssignmentsCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(UpdateClassroomAssignmentsCommand command, CancellationToken cancellationToken)
    {
        await CreateClassroomCommandHandler.ValidateTeacherAndCourse(
            dbContext, command.TeacherId, command.CourseId, cancellationToken);

        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        classroom.TeacherId = command.TeacherId;
        classroom.CourseId = command.CourseId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }
}

public sealed class AddStudentToClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<AddStudentToClassroomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(AddStudentToClassroomCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var student = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Id == command.StudentId && x.Role == UserRole.Student, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var exists = await dbContext.ClassroomStudents.AnyAsync(
            x => x.ClassroomId == classroom.Id && x.StudentId == student.Id, cancellationToken);
        if (!exists)
        {
            dbContext.ClassroomStudents.Add(new ClassroomStudent
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroom.Id,
                StudentId = student.Id,
                JoinedAtUtc = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }
}

public sealed class UpdateClassroomWhatsAppCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateClassroomWhatsAppCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(UpdateClassroomWhatsAppCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        if (command.WhatsAppGroupInviteUrl is not null)
        {
            classroom.WhatsAppGroupInviteUrl = command.WhatsAppGroupInviteUrl.Trim();
        }

        if (command.WhatsAppNotifyPhones is not null)
        {
            classroom.WhatsAppNotifyPhones = command.WhatsAppNotifyPhones.Trim();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }
}

public sealed class GetClassroomsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetClassroomsQuery, IReadOnlyList<ClassroomDto>>
{
    public async Task<IReadOnlyList<ClassroomDto>> Handle(GetClassroomsQuery query, CancellationToken cancellationToken)
    {
        var classrooms = await dbContext.Classrooms
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .Include(x => x.Students)
                .ThenInclude(x => x.Student)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase))
        {
            classrooms = classrooms.Where(x => x.TeacherId == query.ViewerUserId).ToList();
        }
        else if (string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase))
        {
            classrooms = classrooms
                .Where(x => x.Students.Any(s => s.StudentId == query.ViewerUserId))
                .ToList();
        }
        else if (string.Equals(query.ViewerRole, nameof(UserRole.Parent), StringComparison.OrdinalIgnoreCase))
        {
            var childIds = await dbContext.Users
                .Where(x => x.ParentId == query.ViewerUserId && x.Role == UserRole.Student)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            classrooms = classrooms
                .Where(x => x.Students.Any(s => childIds.Contains(s.StudentId)))
                .ToList();
        }

        return classrooms.Select(CreateClassroomCommandHandler.Map).ToList();
    }
}

public sealed class GetClassroomByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetClassroomByIdQuery, ClassroomDto?>
{
    public Task<ClassroomDto?> Handle(GetClassroomByIdQuery query, CancellationToken cancellationToken) =>
        CreateClassroomCommandHandler.LoadDto(dbContext, query.ClassroomId, cancellationToken);
}

public sealed record UpdateClassroomRequest(
    string Name,
    string? Description,
    Guid? TeacherId,
    Guid? CourseId,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones);

public sealed record UpdateClassroomCommand(
    Guid ClassroomId,
    string Name,
    string? Description,
    Guid? TeacherId,
    Guid? CourseId,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones) : ICommand<ClassroomDto>;

public sealed record DeleteClassroomCommand(Guid ClassroomId) : ICommand<bool>;

public sealed record RemoveStudentFromClassroomCommand(Guid ClassroomId, Guid StudentId) : ICommand<ClassroomDto>;

public sealed class UpdateClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateClassroomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(UpdateClassroomCommand command, CancellationToken cancellationToken)
    {
        await CreateClassroomCommandHandler.ValidateTeacherAndCourse(
            dbContext, command.TeacherId, command.CourseId, cancellationToken);

        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var name = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Classroom name is required.");
        }

        classroom.Name = name;
        classroom.Description = (command.Description ?? string.Empty).Trim();
        classroom.TeacherId = command.TeacherId;
        classroom.CourseId = command.CourseId;
        classroom.WhatsAppGroupInviteUrl = (command.WhatsAppGroupInviteUrl ?? string.Empty).Trim();
        classroom.WhatsAppNotifyPhones = (command.WhatsAppNotifyPhones ?? string.Empty).Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }
}

public sealed class DeleteClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteClassroomCommand, bool>
{
    public async Task<bool> Handle(DeleteClassroomCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var memberships = await dbContext.ClassroomStudents
            .Where(x => x.ClassroomId == classroom.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomStudents.RemoveRange(memberships);

        var sessions = await dbContext.LiveSessions
            .Where(x => x.ClassroomId == classroom.Id)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.ClassroomId = null;
        }

        var quizzes = await dbContext.Quizzes.Where(x => x.ClassroomId == classroom.Id).ToListAsync(cancellationToken);
        foreach (var quiz in quizzes)
        {
            quiz.ClassroomId = null;
        }

        var assignments = await dbContext.Assignments.Where(x => x.ClassroomId == classroom.Id).ToListAsync(cancellationToken);
        dbContext.Assignments.RemoveRange(assignments);

        dbContext.Classrooms.Remove(classroom);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class RemoveStudentFromClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<RemoveStudentFromClassroomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(RemoveStudentFromClassroomCommand command, CancellationToken cancellationToken)
    {
        var membership = await dbContext.ClassroomStudents.FirstOrDefaultAsync(
            x => x.ClassroomId == command.ClassroomId && x.StudentId == command.StudentId,
            cancellationToken)
            ?? throw new InvalidOperationException("Student is not enrolled in this classroom.");

        dbContext.ClassroomStudents.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, command.ClassroomId, cancellationToken))!;
    }
}
