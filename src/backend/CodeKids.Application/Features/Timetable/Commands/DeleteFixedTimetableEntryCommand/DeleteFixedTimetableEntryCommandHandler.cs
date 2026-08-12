using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Timetable;

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
