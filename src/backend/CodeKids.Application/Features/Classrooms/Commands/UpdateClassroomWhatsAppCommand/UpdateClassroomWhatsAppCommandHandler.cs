using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

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
