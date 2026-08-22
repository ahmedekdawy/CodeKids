using CodeKids.Application.Abstractions;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Timetable;

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
            command.CombinedGrades,
            cancellationToken);

        entry.TeacherId = command.TeacherId;
        entry.CourseId = command.CourseId;
        entry.DayOfWeek = command.DayOfWeek;
        entry.SessionNumber = command.SessionNumber;
        entry.Period = command.Period;
        entry.CombinedGrades = GradeStageHelper.SerializeGradeCodes(command.CombinedGrades);

        await dbContext.SaveChangesAsync(cancellationToken);
        return await FixedTimetableValidators.LoadDtoAsync(dbContext, entry.Id, cancellationToken);
    }
}
