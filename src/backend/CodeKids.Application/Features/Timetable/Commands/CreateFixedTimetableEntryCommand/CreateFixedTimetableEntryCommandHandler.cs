using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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
        try
        {

        
        await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {

            throw ex;
        }
        return await FixedTimetableValidators.LoadDtoAsync(dbContext, entry.Id, cancellationToken);
    }
}
