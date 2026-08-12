using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public sealed class RunDailyWhatsAppReportsCommandHandler(
    IAppDbContext dbContext,
    IWhatsAppClient whatsAppClient)
    : ICommandHandler<RunDailyWhatsAppReportsCommand, DailyWhatsAppReportsResultDto>
{
    public async Task<DailyWhatsAppReportsResultDto> Handle(
        RunDailyWhatsAppReportsCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.Force)
        {
            var already = await dbContext.WhatsAppReportLogs.AnyAsync(
                x => x.ReportType == "DailyDigest"
                     && x.SentAtUtc.Date == DateTime.UtcNow.Date
                     && x.Status != "Skipped",
                cancellationToken);
            if (already)
            {
                return new DailyWhatsAppReportsResultDto(0, 0, 0, 0, 1);
            }
        }

        var classrooms = await dbContext.Classrooms
            .Include(x => x.Students)
            .Where(x => x.DailyWhatsAppReportsEnabled)
            .ToListAsync(cancellationToken);

        var studentIds = classrooms.SelectMany(c => c.Students.Select(s => s.StudentId)).Distinct().ToList();
        var students = await dbContext.Users
            .Include(x => x.Parent)
            .Where(x => studentIds.Contains(x.Id) && x.Role == UserRole.Student)
            .ToListAsync(cancellationToken);

        var attemptedStudent = 0;
        var attemptedParent = 0;
        var sent = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var student in students)
        {
            var message = await AnalyticsQueries.BuildStudentDigestAsync(dbContext, student, cancellationToken);
            var classroomId = classrooms
                .FirstOrDefault(c => c.Students.Any(s => s.StudentId == student.Id))?.Id;

            if (!string.IsNullOrWhiteSpace(student.MobilePhone))
            {
                attemptedStudent++;
                var result = await whatsAppClient.SendTextAsync(student.MobilePhone, message, cancellationToken);
                await LogAsync(dbContext, classroomId, student.Id, "DailyDigest", student.MobilePhone,
                    result.Sent ? "Sent" : "Failed", Truncate(result.Sent ? message : result.Detail), cancellationToken);
                if (result.Sent) sent++;
                else failed++;
            }
            else
            {
                skipped++;
            }

            var parentPhone = student.Parent?.MobilePhone?.Trim();
            if (!string.IsNullOrWhiteSpace(parentPhone))
            {
                attemptedParent++;
                var parentMessage =
                    $"Parent update — {student.DisplayName}\n" + message;
                var result = await whatsAppClient.SendTextAsync(parentPhone, parentMessage, cancellationToken);
                await LogAsync(dbContext, classroomId, student.Id, "DailyDigestParent", parentPhone,
                    result.Sent ? "Sent" : "Failed", Truncate(result.Sent ? parentMessage : result.Detail), cancellationToken);
                if (result.Sent) sent++;
                else failed++;
            }
        }

        if (students.Count == 0)
        {
            dbContext.WhatsAppReportLogs.Add(new WhatsAppReportLog
            {
                Id = Guid.NewGuid(),
                ReportType = "DailyDigest",
                RecipientPhone = "",
                Status = "Skipped",
                MessagePreview = "No enrolled students in classrooms with daily reports enabled.",
                SentAtUtc = DateTimeOffset.UtcNow
            });
            skipped++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DailyWhatsAppReportsResultDto(attemptedStudent, attemptedParent, sent, failed, skipped);
    }

    private static async Task LogAsync(
        IAppDbContext dbContext,
        Guid? classroomId,
        Guid? studentId,
        string reportType,
        string phone,
        string status,
        string preview,
        CancellationToken cancellationToken)
    {
        dbContext.WhatsAppReportLogs.Add(new WhatsAppReportLog
        {
            Id = Guid.NewGuid(),
            ClassroomId = classroomId,
            StudentId = studentId,
            ReportType = reportType,
            RecipientPhone = phone,
            Status = status,
            MessagePreview = preview,
            SentAtUtc = DateTimeOffset.UtcNow
        });
        await Task.CompletedTask;
    }

    private static string Truncate(string value) =>
        value.Length <= 1000 ? value : value[..997] + "...";
}
