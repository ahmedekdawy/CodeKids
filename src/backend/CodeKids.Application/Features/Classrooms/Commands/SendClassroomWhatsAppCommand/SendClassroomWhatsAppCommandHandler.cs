using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

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
