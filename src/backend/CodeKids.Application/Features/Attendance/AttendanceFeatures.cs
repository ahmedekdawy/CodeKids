using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed record TeacherSessionAttendanceDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid CourseId,
    string CourseName,
    int? CourseGrade,
    DateOnly SessionDate,
    string Label);

public sealed record CreateTeacherSessionAttendanceRequest(
    Guid TeacherId,
    Guid CourseId,
    DateOnly SessionDate);

public sealed record CreateMyTeacherSessionAttendanceRequest(
    Guid CourseId,
    DateOnly SessionDate);

public sealed record CreateTeacherSessionAttendanceCommand(
    Guid TeacherId,
    Guid CourseId,
    DateOnly SessionDate) : ICommand<TeacherSessionAttendanceDto>;

public sealed record ListTeacherSessionAttendanceQuery(
    Guid? TeacherId = null,
    int? CourseGrade = null,
    DateOnly? SessionDate = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IQuery<IReadOnlyList<TeacherSessionAttendanceDto>>;

public sealed record DeleteTeacherSessionAttendanceCommand(
    Guid AttendanceId,
    Guid? ActingTeacherId = null) : ICommand<bool>;

public sealed class ListTeacherSessionAttendanceQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListTeacherSessionAttendanceQuery, IReadOnlyList<TeacherSessionAttendanceDto>>
{
    public async Task<IReadOnlyList<TeacherSessionAttendanceDto>> Handle(
        ListTeacherSessionAttendanceQuery query,
        CancellationToken cancellationToken)
    {
        var rows = dbContext.TeacherSessionAttendances
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .AsQueryable();

        if (query.TeacherId.HasValue)
        {
            rows = rows.Where(x => x.TeacherId == query.TeacherId.Value);
        }

        if (query.CourseGrade.HasValue)
        {
            rows = rows.Where(x => x.Course != null && x.Course.Grade == query.CourseGrade.Value);
        }

        if (query.SessionDate.HasValue)
        {
            rows = rows.Where(x => x.SessionDate == query.SessionDate.Value);
        }
        else
        {
            if (query.FromDate.HasValue)
            {
                rows = rows.Where(x => x.SessionDate >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                rows = rows.Where(x => x.SessionDate <= query.ToDate.Value);
            }
        }

        return (await rows
            .OrderByDescending(x => x.SessionDate)
            .ThenBy(x => x.Teacher!.DisplayName)
            .ThenBy(x => x.Course!.Grade)
            .ThenBy(x => x.Course!.Title)
            .ToListAsync(cancellationToken))
            .Select(TeacherSessionAttendanceValidators.ToDto)
            .ToList();
    }
}

public sealed class CreateTeacherSessionAttendanceCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateTeacherSessionAttendanceCommand, TeacherSessionAttendanceDto>
{
    public async Task<TeacherSessionAttendanceDto> Handle(
        CreateTeacherSessionAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        await TeacherSessionAttendanceValidators.ValidateAsync(
            dbContext,
            command.TeacherId,
            command.CourseId,
            command.SessionDate,
            cancellationToken);

        var row = new TeacherSessionAttendance
        {
            Id = Guid.NewGuid(),
            TeacherId = command.TeacherId,
            CourseId = command.CourseId,
            SessionDate = command.SessionDate,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.TeacherSessionAttendances.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await TeacherSessionAttendanceValidators.LoadDtoAsync(dbContext, row.Id, cancellationToken);
    }
}

public sealed class DeleteTeacherSessionAttendanceCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteTeacherSessionAttendanceCommand, bool>
{
    public async Task<bool> Handle(
        DeleteTeacherSessionAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.TeacherSessionAttendances
            .FirstOrDefaultAsync(x => x.Id == command.AttendanceId, cancellationToken)
            ?? throw new InvalidOperationException("Session attendance not found.");

        if (command.ActingTeacherId.HasValue && row.TeacherId != command.ActingTeacherId.Value)
        {
            throw new InvalidOperationException("You can only remove your own attendance records.");
        }

        dbContext.TeacherSessionAttendances.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class TeacherSessionAttendanceValidators
{
    public static async Task ValidateAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        Guid courseId,
        DateOnly sessionDate,
        CancellationToken cancellationToken)
    {
        if (sessionDate == default)
        {
            throw new InvalidOperationException("Session date is required.");
        }

        var teacher = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == teacherId, cancellationToken)
            ?? throw new InvalidOperationException("Teacher not found.");

        if (teacher.Role != UserRole.Teacher)
        {
            throw new InvalidOperationException("Selected user must be a teacher.");
        }

        _ = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var duplicate = await dbContext.TeacherSessionAttendances
            .AsNoTracking()
            .AnyAsync(
                x => x.TeacherId == teacherId
                     && x.CourseId == courseId
                     && x.SessionDate == sessionDate,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Attendance for this teacher, course, and date already exists.");
        }
    }

    public static async Task<TeacherSessionAttendanceDto> LoadDtoAsync(
        IAppDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.TeacherSessionAttendances
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return ToDto(row);
    }

    public static TeacherSessionAttendanceDto ToDto(TeacherSessionAttendance row)
    {
        var teacherName = row.Teacher?.DisplayName ?? string.Empty;
        var courseName = row.Course?.Title ?? string.Empty;
        var courseGrade = row.Course?.Grade;
        var gradeLabel = courseGrade switch
        {
            null => "All",
            -1 => "KG1",
            0 => "KG2",
            _ => $"Grade {courseGrade}"
        };
        var label = string.Join(
            " - ",
            new[] { gradeLabel, courseName, teacherName, row.SessionDate.ToString("yyyy-MM-dd") }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        return new TeacherSessionAttendanceDto(
            row.Id,
            row.TeacherId,
            teacherName,
            row.CourseId,
            courseName,
            courseGrade,
            row.SessionDate,
            label);
    }
}
