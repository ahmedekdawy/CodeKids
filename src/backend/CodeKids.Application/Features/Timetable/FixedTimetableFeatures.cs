using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Timetable;

public sealed record FixedTimetableEntryDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid CourseId,
    string CourseName,
    int? CourseGrade,
    int DayOfWeek,
    int SessionNumber,
    string Period,
    string Label);

public sealed record CreateFixedTimetableEntryRequest(
    Guid TeacherId,
    Guid CourseId,
    int DayOfWeek,
    int SessionNumber,
    string Period);

public sealed record UpdateFixedTimetableEntryRequest(
    Guid TeacherId,
    Guid CourseId,
    int DayOfWeek,
    int SessionNumber,
    string Period);

public sealed record ListFixedTimetableEntriesQuery(
    Guid? TeacherId = null,
    int? CourseGrade = null,
    TimetablePeriod? Period = null) : IQuery<IReadOnlyList<FixedTimetableEntryDto>>;

public sealed record CreateFixedTimetableEntryCommand(
    Guid TeacherId,
    Guid CourseId,
    int DayOfWeek,
    int SessionNumber,
    TimetablePeriod Period) : ICommand<FixedTimetableEntryDto>;

public sealed record UpdateFixedTimetableEntryCommand(
    Guid EntryId,
    Guid TeacherId,
    Guid CourseId,
    int DayOfWeek,
    int SessionNumber,
    TimetablePeriod Period) : ICommand<FixedTimetableEntryDto>;

public sealed record DeleteFixedTimetableEntryCommand(Guid EntryId) : ICommand<bool>;

public sealed class ListFixedTimetableEntriesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListFixedTimetableEntriesQuery, IReadOnlyList<FixedTimetableEntryDto>>
{
    public async Task<IReadOnlyList<FixedTimetableEntryDto>> Handle(
        ListFixedTimetableEntriesQuery query,
        CancellationToken cancellationToken)
    {
        var entries = dbContext.FixedTimetableEntries
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .AsQueryable();

        if (query.TeacherId.HasValue)
        {
            entries = entries.Where(x => x.TeacherId == query.TeacherId.Value);
        }

        if (query.CourseGrade.HasValue)
        {
            entries = entries.Where(x => x.Course != null && x.Course.Grade == query.CourseGrade.Value);
        }

        if (query.Period.HasValue)
        {
            entries = entries.Where(x => x.Period == query.Period.Value);
        }

        return (await entries
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.Period)
            .ThenBy(x => x.SessionNumber)
            .ThenBy(x => x.Teacher!.DisplayName)
            .ToListAsync(cancellationToken))
            .Select(FixedTimetableValidators.ToDto)
            .ToList();
    }
}

public sealed class CreateFixedTimetableEntryCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateFixedTimetableEntryCommand, FixedTimetableEntryDto>
{
    public async Task<FixedTimetableEntryDto> Handle(
        CreateFixedTimetableEntryCommand command,
        CancellationToken cancellationToken)
    {
        await FixedTimetableValidators.ValidateAsync(
            dbContext,
            command.TeacherId,
            command.CourseId,
            command.DayOfWeek,
            command.SessionNumber,
            command.Period,
            excludeEntryId: null,
            cancellationToken);

        var entry = new FixedTimetableEntry
        {
            Id = Guid.NewGuid(),
            TeacherId = command.TeacherId,
            CourseId = command.CourseId,
            DayOfWeek = command.DayOfWeek,
            SessionNumber = command.SessionNumber,
            Period = command.Period,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.FixedTimetableEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await FixedTimetableValidators.LoadDtoAsync(dbContext, entry.Id, cancellationToken);
    }
}

public sealed class UpdateFixedTimetableEntryCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateFixedTimetableEntryCommand, FixedTimetableEntryDto>
{
    public async Task<FixedTimetableEntryDto> Handle(
        UpdateFixedTimetableEntryCommand command,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.FixedTimetableEntries
            .FirstOrDefaultAsync(x => x.Id == command.EntryId, cancellationToken)
            ?? throw new InvalidOperationException("Timetable entry not found.");

        await FixedTimetableValidators.ValidateAsync(
            dbContext,
            command.TeacherId,
            command.CourseId,
            command.DayOfWeek,
            command.SessionNumber,
            command.Period,
            excludeEntryId: command.EntryId,
            cancellationToken);

        entry.TeacherId = command.TeacherId;
        entry.CourseId = command.CourseId;
        entry.DayOfWeek = command.DayOfWeek;
        entry.SessionNumber = command.SessionNumber;
        entry.Period = command.Period;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await FixedTimetableValidators.LoadDtoAsync(dbContext, entry.Id, cancellationToken);
    }
}

public sealed class DeleteFixedTimetableEntryCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteFixedTimetableEntryCommand, bool>
{
    public async Task<bool> Handle(DeleteFixedTimetableEntryCommand command, CancellationToken cancellationToken)
    {
        var entry = await dbContext.FixedTimetableEntries
            .FirstOrDefaultAsync(x => x.Id == command.EntryId, cancellationToken)
            ?? throw new InvalidOperationException("Timetable entry not found.");

        dbContext.FixedTimetableEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class FixedTimetableValidators
{
    public static async Task ValidateAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        Guid courseId,
        int dayOfWeek,
        int sessionNumber,
        TimetablePeriod period,
        Guid? excludeEntryId,
        CancellationToken cancellationToken)
    {
        if (dayOfWeek is < 0 or > 6)
        {
            throw new InvalidOperationException("Day of week must be between 0 (Sunday) and 6 (Saturday).");
        }

        if (sessionNumber is < 1 or > 6)
        {
            throw new InvalidOperationException("Session number must be between 1 and 6.");
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

        var overlap = await dbContext.FixedTimetableEntries
            .AsNoTracking()
            .AnyAsync(
                x => x.TeacherId == teacherId
                     && x.DayOfWeek == dayOfWeek
                     && x.SessionNumber == sessionNumber
                     && x.Period == period
                     && (excludeEntryId == null || x.Id != excludeEntryId),
                cancellationToken);

        if (overlap)
        {
            throw new InvalidOperationException("Teacher already has a timetable session in this slot.");
        }
    }

    public static async Task<FixedTimetableEntryDto> LoadDtoAsync(
        IAppDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.FixedTimetableEntries
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return ToDto(entry);
    }

    public static FixedTimetableEntryDto ToDto(FixedTimetableEntry entry)
    {
        var teacherName = entry.Teacher?.DisplayName ?? string.Empty;
        var courseName = entry.Course?.Title ?? string.Empty;
        var courseGrade = entry.Course?.Grade;
        var gradeLabel = courseGrade switch
        {
            null => "All",
            -1 => "KG1",
            0 => "KG2",
            _ => $"Grade {courseGrade}"
        };
        var label = string.Join(
            " - ",
            new[] { gradeLabel, courseName, teacherName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new FixedTimetableEntryDto(
            entry.Id,
            entry.TeacherId,
            teacherName,
            entry.CourseId,
            courseName,
            courseGrade,
            entry.DayOfWeek,
            entry.SessionNumber,
            entry.Period == TimetablePeriod.Pm ? "pm" : "am",
            label);
    }
}

public static class TimetablePeriodParser
{
    public static TimetablePeriod Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Period must be am or pm.");
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "am" => TimetablePeriod.Am,
            "pm" => TimetablePeriod.Pm,
            _ => throw new InvalidOperationException("Period must be am or pm.")
        };
    }

    public static TimetablePeriod? ParseOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Parse(value);
    }
}
