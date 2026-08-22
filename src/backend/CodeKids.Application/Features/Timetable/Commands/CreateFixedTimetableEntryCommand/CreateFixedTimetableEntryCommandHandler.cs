using CodeKids.Application.Abstractions;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;

namespace CodeKids.Application.Features.Timetable;

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
            command.CombinedGrades,
            cancellationToken);

        var entry = new FixedTimetableEntry
        {
            Id = Guid.NewGuid(),
            TeacherId = command.TeacherId,
            CourseId = command.CourseId,
            DayOfWeek = command.DayOfWeek,
            SessionNumber = command.SessionNumber,
            Period = command.Period,
            CombinedGrades = GradeStageHelper.SerializeGradeCodes(command.CombinedGrades),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.FixedTimetableEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await FixedTimetableValidators.LoadDtoAsync(dbContext, entry.Id, cancellationToken);
    }
}
