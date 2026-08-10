using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record ClassroomStudentDto(Guid StudentId, string DisplayName, string Email, string MobilePhone);

public sealed record ClassroomTeacherDto(Guid TeacherId, string DisplayName);

public sealed record ClassroomCourseDto(
    Guid CourseId,
    string CourseTitle,
    int? CourseGrade,
    Guid TeacherId,
    string TeacherName);

public sealed record ClassroomCourseAssignmentRequest(Guid CourseId, Guid TeacherId);

public sealed record ClassroomDto(
    Guid Id,
    string Name,
    string Description,
    int? Grade,
    IReadOnlyList<ClassroomTeacherDto> Teachers,
    IReadOnlyList<ClassroomCourseDto> Courses,
    Guid? CourseId,
    string? CourseTitle,
    int? CourseGrade,
    string WhatsAppGroupInviteUrl,
    string WhatsAppNotifyPhones,
    bool DailyWhatsAppReportsEnabled,
    IReadOnlyList<ClassroomStudentDto> Students);

public sealed record EnrollStudentResultDto(ClassroomDto Classroom, string WhatsAppStatus);

public sealed record SendClassroomWhatsAppRequest(
    string Message,
    IReadOnlyList<Guid>? StudentIds,
    bool IncludeGroupInviteLink = true);

public sealed record SendClassroomWhatsAppCommand(
    Guid TeacherUserId,
    Guid ClassroomId,
    string Message,
    IReadOnlyList<Guid>? StudentIds,
    bool IncludeGroupInviteLink) : ICommand<SendClassroomWhatsAppResultDto>;

public sealed record SendClassroomWhatsAppResultDto(
    int SentCount,
    int FailedCount,
    string Status,
    string? GroupShareUrl);

public sealed record CreateClassroomRequest(
    string Name,
    string? Description,
    int? Grade,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones);

public sealed record AssignClassroomRequest(IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses);

public sealed record AddClassroomStudentRequest(Guid StudentId);

public sealed record CreateClassroomCommand(
    string Name,
    string? Description,
    int? Grade,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones) : ICommand<ClassroomDto>;

public sealed record UpdateClassroomAssignmentsCommand(
    Guid ClassroomId,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses) : ICommand<ClassroomDto>;

public sealed record AddStudentToClassroomCommand(Guid ClassroomId, Guid StudentId) : ICommand<EnrollStudentResultDto>;

public sealed record UpdateClassroomWhatsAppRequest(
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones,
    bool? DailyWhatsAppReportsEnabled);

public sealed record UpdateClassroomWhatsAppCommand(
    Guid ClassroomId,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones,
    bool? DailyWhatsAppReportsEnabled) : ICommand<ClassroomDto>;

public sealed record GetClassroomsQuery(Guid ViewerUserId, string ViewerRole) : IQuery<IReadOnlyList<ClassroomDto>>;

public sealed record GetClassroomByIdQuery(Guid ClassroomId) : IQuery<ClassroomDto?>;

public sealed class CreateClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateClassroomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(CreateClassroomCommand command, CancellationToken cancellationToken)
    {
        var grade = CreateCourseCommandHandler.NormalizeGrade(command.Grade);
        var assignments = await ValidateCourseAssignments(dbContext, command.Courses, grade, cancellationToken);

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Description = (command.Description ?? string.Empty).Trim(),
            Grade = grade,
            CourseId = assignments.Count > 0 ? assignments[0].CourseId : null,
            WhatsAppGroupInviteUrl = (command.WhatsAppGroupInviteUrl ?? string.Empty).Trim(),
            WhatsAppNotifyPhones = (command.WhatsAppNotifyPhones ?? string.Empty).Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (string.IsNullOrWhiteSpace(classroom.Name))
        {
            throw new InvalidOperationException("Classroom name is required.");
        }

        dbContext.Classrooms.Add(classroom);
        await ReplaceCourseAssignmentsAsync(dbContext, classroom.Id, assignments, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }

    internal static async Task<IReadOnlyList<ClassroomCourseAssignmentRequest>> ValidateCourseAssignments(
        IAppDbContext dbContext,
        IReadOnlyList<ClassroomCourseAssignmentRequest>? courses,
        int? classroomGrade,
        CancellationToken cancellationToken)
    {
        var assignments = (courses ?? [])
            .Where(x => x.CourseId != Guid.Empty && x.TeacherId != Guid.Empty)
            .GroupBy(x => x.CourseId)
            .Select(g => g.Last())
            .ToList();

        if (assignments.Count == 0) return assignments;

        var courseIds = assignments.Select(x => x.CourseId).ToList();
        var teacherIds = assignments.Select(x => x.TeacherId).Distinct().ToList();

        var matchedCourses = await dbContext.Courses
            .AsNoTracking()
            .Where(x => courseIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Grade })
            .ToListAsync(cancellationToken);
        if (matchedCourses.Count != courseIds.Count)
        {
            throw new InvalidOperationException("One or more courses were not found.");
        }

        if (classroomGrade is not null
            && matchedCourses.Any(c => !GradeStageHelper.CourseMatchesClassroomGrade(c.Grade, classroomGrade)))
        {
            throw new InvalidOperationException("One or more courses do not match the classroom grade.");
        }

        var teachers = await dbContext.Users
            .AsNoTracking()
            .Where(x => teacherIds.Contains(x.Id) && x.Role == UserRole.Teacher)
            .Select(x => new { x.Id, x.Stages })
            .ToListAsync(cancellationToken);
        if (teachers.Count != teacherIds.Count)
        {
            throw new InvalidOperationException("One or more teachers were not found.");
        }

        if (classroomGrade is not null
            && teachers.Any(t => !GradeStageHelper.TeacherCoversStage(t.Stages, classroomGrade)))
        {
            throw new InvalidOperationException("One or more teachers are not assigned to this classroom stage.");
        }

        return assignments;
    }

    internal static async Task ReplaceCourseAssignmentsAsync(
        IAppDbContext dbContext,
        Guid classroomId,
        IReadOnlyList<ClassroomCourseAssignmentRequest> assignments,
        CancellationToken cancellationToken)
    {
        var existingCourses = await dbContext.ClassroomCourses
            .Where(x => x.ClassroomId == classroomId)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomCourses.RemoveRange(existingCourses);

        var now = DateTimeOffset.UtcNow;
        foreach (var item in assignments)
        {
            dbContext.ClassroomCourses.Add(new ClassroomCourse
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroomId,
                CourseId = item.CourseId,
                TeacherId = item.TeacherId,
                AssignedAtUtc = now
            });
        }
    }

    internal static async Task<ClassroomDto?> LoadDto(IAppDbContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .AsNoTracking()
            .Include(x => x.Courses)
                .ThenInclude(x => x.Course)
            .Include(x => x.Courses)
                .ThenInclude(x => x.Teacher)
            .Include(x => x.Course)
            .Include(x => x.Students)
                .ThenInclude(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return classroom is null ? null : Map(classroom);
    }

    internal static bool HasTeacher(Classroom classroom, Guid teacherUserId) =>
        classroom.Courses.Any(x => x.TeacherId == teacherUserId);

    internal static ClassroomDto Map(Classroom classroom)
    {
        var courses = classroom.Courses
            .Where(x => x.Course is not null && x.Teacher is not null)
            .OrderBy(x => x.Course!.Grade ?? 999)
            .ThenBy(x => x.Course!.Title)
            .Select(x => new ClassroomCourseDto(
                x.CourseId,
                x.Course!.Title,
                x.Course.Grade,
                x.TeacherId,
                x.Teacher!.DisplayName))
            .ToList();

        var primary = courses.FirstOrDefault();
        var teachers = courses
            .GroupBy(x => x.TeacherId)
            .Select(g => new ClassroomTeacherDto(g.Key, g.First().TeacherName))
            .OrderBy(x => x.DisplayName)
            .ToList();

        return new(
            classroom.Id,
            classroom.Name,
            classroom.Description,
            classroom.Grade,
            teachers,
            courses,
            primary?.CourseId ?? classroom.CourseId,
            primary?.CourseTitle ?? classroom.Course?.Title,
            primary?.CourseGrade ?? classroom.Course?.Grade,
            classroom.WhatsAppGroupInviteUrl,
            classroom.WhatsAppNotifyPhones,
            classroom.DailyWhatsAppReportsEnabled,
            classroom.Students
                .Where(x => x.Student is not null)
                .Select(x => new ClassroomStudentDto(
                    x.StudentId,
                    x.Student!.DisplayName,
                    x.Student.Email,
                    x.Student.MobilePhone))
                .OrderBy(x => x.DisplayName)
                .ToList());
    }
}

public sealed class UpdateClassroomAssignmentsCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateClassroomAssignmentsCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(UpdateClassroomAssignmentsCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var assignments = await CreateClassroomCommandHandler.ValidateCourseAssignments(
            dbContext, command.Courses, classroom.Grade, cancellationToken);

        classroom.CourseId = assignments.Count > 0 ? assignments[0].CourseId : null;
        await CreateClassroomCommandHandler.ReplaceCourseAssignmentsAsync(
            dbContext, classroom.Id, assignments, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }
}

public sealed class AddStudentToClassroomCommandHandler(
    IAppDbContext dbContext,
    IWhatsAppClient whatsAppClient) : ICommandHandler<AddStudentToClassroomCommand, EnrollStudentResultDto>
{
    public async Task<EnrollStudentResultDto> Handle(AddStudentToClassroomCommand command, CancellationToken cancellationToken)
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
        }

        var whatsAppStatus = "No student mobile on file.";
        var mobile = (student.MobilePhone ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(mobile))
        {
            classroom.WhatsAppNotifyPhones = MergePhones(classroom.WhatsAppNotifyPhones, mobile);

            var invite = classroom.WhatsAppGroupInviteUrl?.Trim() ?? string.Empty;
            var message = string.IsNullOrWhiteSpace(invite)
                ? $"CodeKids: You were enrolled in classroom \"{classroom.Name}\"."
                : $"CodeKids: You were enrolled in classroom \"{classroom.Name}\".\nJoin the WhatsApp group:\n{invite}";

            var send = await whatsAppClient.SendTextAsync(mobile, message, cancellationToken);
            whatsAppStatus = string.IsNullOrWhiteSpace(invite)
                ? $"Added {mobile} to classroom notify list. Invite link not set. WhatsApp: {send.Detail}"
                : $"Added {mobile} to classroom notify list and sent group invite. WhatsApp: {send.Detail}";
        }
        else if (!string.IsNullOrWhiteSpace(classroom.WhatsAppGroupInviteUrl))
        {
            whatsAppStatus = "Student enrolled, but has no mobile number — could not send WhatsApp group invite.";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
        return new EnrollStudentResultDto(dto, whatsAppStatus);
    }

    internal static string MergePhones(string existing, string phone)
    {
        var phones = existing
            .Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (!phones.Any(p => string.Equals(p, phone, StringComparison.OrdinalIgnoreCase)))
        {
            phones.Add(phone);
        }

        return string.Join(", ", phones);
    }
}

public sealed class SendClassroomWhatsAppCommandHandler(
    IAppDbContext dbContext,
    IWhatsAppClient whatsAppClient) : ICommandHandler<SendClassroomWhatsAppCommand, SendClassroomWhatsAppResultDto>
{
    public async Task<SendClassroomWhatsAppResultDto> Handle(
        SendClassroomWhatsAppCommand command,
        CancellationToken cancellationToken)
    {
        var message = command.Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException("Message is required.");
        }

        var classroom = await dbContext.Classrooms
            .Include(x => x.Courses)
            .Include(x => x.Students)
                .ThenInclude(s => s.Student)
            .FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        if (!CreateClassroomCommandHandler.HasTeacher(classroom, command.TeacherUserId))
        {
            throw new InvalidOperationException("Only an assigned classroom teacher can message this class.");
        }

        var body = message;
        if (command.IncludeGroupInviteLink && !string.IsNullOrWhiteSpace(classroom.WhatsAppGroupInviteUrl))
        {
            body += $"\n\nClass WhatsApp group: {classroom.WhatsAppGroupInviteUrl}";
        }

        var groupShareUrl = whatsAppClient.BuildShareUrl(body);

        IEnumerable<ClassroomStudent> memberships = classroom.Students;
        if (command.StudentIds is { Count: > 0 })
        {
            var selected = command.StudentIds.ToHashSet();
            memberships = memberships.Where(x => selected.Contains(x.StudentId));
        }

        var phones = memberships
            .Select(x => x.Student?.MobilePhone?.Trim() ?? string.Empty)
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (phones.Count == 0)
        {
            return new SendClassroomWhatsAppResultDto(
                0,
                0,
                "No selected students have a mobile number. Use the group share link instead.",
                groupShareUrl);
        }

        var sent = 0;
        var failed = 0;
        var details = new List<string>();
        foreach (var phone in phones)
        {
            var result = await whatsAppClient.SendTextAsync(phone, body, cancellationToken);
            details.Add($"{phone}: {result.Detail}");
            if (result.Sent) sent++;
            else failed++;
        }

        return new SendClassroomWhatsAppResultDto(
            sent,
            failed,
            string.Join(" | ", details),
            groupShareUrl);
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

        if (command.DailyWhatsAppReportsEnabled is bool enabled)
        {
            classroom.DailyWhatsAppReportsEnabled = enabled;
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
            .Include(x => x.Courses)
                .ThenInclude(x => x.Course)
            .Include(x => x.Courses)
                .ThenInclude(x => x.Teacher)
            .Include(x => x.Course)
            .Include(x => x.Students)
                .ThenInclude(x => x.Student)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase))
        {
            classrooms = classrooms
                .Where(x => CreateClassroomCommandHandler.HasTeacher(x, query.ViewerUserId))
                .ToList();
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
    int? Grade,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones);

public sealed record UpdateClassroomCommand(
    Guid ClassroomId,
    string Name,
    string? Description,
    int? Grade,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones) : ICommand<ClassroomDto>;

public sealed record DeleteClassroomCommand(Guid ClassroomId) : ICommand<bool>;

public sealed record RemoveStudentFromClassroomCommand(Guid ClassroomId, Guid StudentId) : ICommand<ClassroomDto>;

public sealed class UpdateClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateClassroomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(UpdateClassroomCommand command, CancellationToken cancellationToken)
    {
        var grade = CreateCourseCommandHandler.NormalizeGrade(command.Grade);
        var assignments = await CreateClassroomCommandHandler.ValidateCourseAssignments(
            dbContext, command.Courses, grade, cancellationToken);

        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var name = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Classroom name is required.");
        }

        classroom.Name = name;
        classroom.Description = (command.Description ?? string.Empty).Trim();
        classroom.Grade = grade;
        classroom.CourseId = assignments.Count > 0 ? assignments[0].CourseId : null;
        classroom.WhatsAppGroupInviteUrl = (command.WhatsAppGroupInviteUrl ?? string.Empty).Trim();
        classroom.WhatsAppNotifyPhones = (command.WhatsAppNotifyPhones ?? string.Empty).Trim();

        await CreateClassroomCommandHandler.ReplaceCourseAssignmentsAsync(
            dbContext, classroom.Id, assignments, cancellationToken);

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

        var courseLinks = await dbContext.ClassroomCourses
            .Where(x => x.ClassroomId == classroom.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomCourses.RemoveRange(courseLinks);

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
