using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Options;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Application.Features.Notifications;

/// <param name="KindLabel">Arabic noun for the assessment, e.g. "واجب".</param>
/// <param name="TargetPath">Frontend path of the assessment, e.g. "/assignments/{id}".</param>
public sealed record AssessmentPublishInfo(
    string Kind,
    string KindLabel,
    Guid EntityId,
    string Title,
    string TargetPath,
    DateTimeOffset? DueAtUtc,
    Guid? ClassroomId,
    Guid? CourseId,
    Guid? CreatedByUserId = null);

/// <summary>
/// Sends the assessment link over WhatsApp to enrolled students, their parents, and classroom
/// teachers when an assessment is published.
/// </summary>
public sealed class AssessmentPublishWhatsAppNotifier(
    IAppDbContext dbContext,
    IWhatsAppMessageSender whatsAppSender,
    IOptions<FrontendOptions> frontendOptions,
    IOptions<NotificationOptions> notificationOptions,
    ILogger<AssessmentPublishWhatsAppNotifier> logger)
{
    public async Task SendAsync(
        AssessmentPublishInfo info,
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        var options = notificationOptions.Value;
        if (!options.WhatsAppOnAssessmentPublish)
        {
            return;
        }

        try
        {
            var link = BuildLink(info.TargetPath);
            var teacherLink = BuildLink(NotificationRecipients.TeacherTargetPath(info.Kind));
            var context = await ResolveContextLabelAsync(info, cancellationToken);
            var alreadySent = new HashSet<string>(StringComparer.Ordinal);

            var students = studentIds.Count == 0
                ? []
                : await dbContext.Users
                    .AsNoTracking()
                    .Include(x => x.Parent)
                    .Where(x => studentIds.Contains(x.Id) && x.IsActive)
                    .ToListAsync(cancellationToken);

            foreach (var student in students)
            {
                await SendToPhoneAsync(
                    student.MobilePhone,
                    BuildStudentMessage(student, info, context, link),
                    info,
                    student.Id,
                    $"{info.Kind}Published",
                    alreadySent,
                    cancellationToken);

                if (options.NotifyParentsOnAssessmentPublish)
                {
                    await SendToPhoneAsync(
                        student.Parent?.MobilePhone,
                        BuildParentMessage(student, info, context, link),
                        info,
                        student.Id,
                        $"{info.Kind}PublishedParent",
                        alreadySent,
                        cancellationToken);
                }
            }

            var teacherIds = await NotificationRecipients.TeachersForAssessmentAsync(
                dbContext,
                info.ClassroomId,
                info.CourseId,
                info.CreatedByUserId,
                cancellationToken);
            if (teacherIds.Count > 0)
            {
                var teachers = await dbContext.Users
                    .AsNoTracking()
                    .Where(x => teacherIds.Contains(x.Id) && x.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var teacher in teachers)
                {
                    await SendToPhoneAsync(
                        teacher.MobilePhone,
                        BuildTeacherMessage(teacher, info, context, link, teacherLink),
                        info,
                        logStudentId: null,
                        $"{info.Kind}PublishedTeacher",
                        alreadySent,
                        cancellationToken);
                }
            }

            // Logging failures must not undo successful sends.
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to persist WhatsApp report logs for {Kind} {Title}.", info.Kind, info.Title);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Failed to send {Kind} publish WhatsApp notifications for {Title}.",
                info.Kind,
                info.Title);
        }
    }

    private async Task SendToPhoneAsync(
        string? phone,
        string message,
        AssessmentPublishInfo info,
        Guid? logStudentId,
        string reportType,
        HashSet<string> alreadySent,
        CancellationToken cancellationToken)
    {
        var trimmed = phone?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        var digits = Digits(trimmed);
        if (digits.Length == 0)
        {
            return;
        }

        if (!alreadySent.Add(digits))
        {
            return;
        }

        var result = await whatsAppSender.SendMessageAsync(
            trimmed,
            message,
            cancellationToken,
            ruleKey: "assessment_published",
            username: "system");

        if (!result.Success)
        {
            logger.LogWarning(
                "{Kind} publish WhatsApp send failed for phone {Phone}: {Error}",
                info.Kind,
                digits,
                result.Error);
        }

        dbContext.WhatsAppReportLogs.Add(new WhatsAppReportLog
        {
            Id = Guid.NewGuid(),
            ClassroomId = info.ClassroomId,
            StudentId = logStudentId,
            ReportType = reportType,
            RecipientPhone = trimmed.Length <= 30 ? trimmed : trimmed[..30],
            Status = result.Success ? "Sent" : "Failed",
            MessagePreview = Truncate(result.Success ? message : result.Error ?? "Send failed."),
            SentAtUtc = DateTimeOffset.UtcNow
        });
    }

    private string BuildLink(string targetPath)
    {
        var baseUrl = (frontendOptions.Value.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
        return $"{baseUrl}{targetPath}";
    }

    /// <summary>Builds the "grade - course" label teachers already see elsewhere in the app.</summary>
    private async Task<string?> ResolveContextLabelAsync(
        AssessmentPublishInfo info,
        CancellationToken cancellationToken)
    {
        var classroom = info.ClassroomId is Guid classroomId
            ? await dbContext.Classrooms
                .AsNoTracking()
                .Where(x => x.Id == classroomId)
                .Select(x => new { x.Name, x.Grade, x.CourseId })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // Assignments carry no course of their own, so fall back to the classroom's course.
        var courseId = info.CourseId ?? classroom?.CourseId;
        var course = courseId is Guid id
            ? await dbContext.Courses
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.Title, x.Grade })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var parts = new List<string>();

        var gradeLabel = await ResolveGradeLabelAsync(course?.Grade ?? classroom?.Grade, cancellationToken);
        if (!string.IsNullOrWhiteSpace(gradeLabel))
        {
            parts.Add(gradeLabel);
        }

        if (!string.IsNullOrWhiteSpace(course?.Title))
        {
            parts.Add(course.Title);
        }
        else if (!string.IsNullOrWhiteSpace(classroom?.Name))
        {
            parts.Add(classroom.Name);
        }

        return parts.Count == 0 ? null : string.Join(" - ", parts);
    }

    private async Task<string?> ResolveGradeLabelAsync(int? gradeCode, CancellationToken cancellationToken)
    {
        if (gradeCode is not int code)
        {
            return null;
        }

        var name = await dbContext.Grades
            .AsNoTracking()
            .Where(x => x.Id == code)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return code switch
        {
            -1 => "KG1",
            0 => "KG2",
            _ => $"الصف {code}"
        };
    }

    // Keep the URL alone on its own lines so WhatsApp still auto-links it next to Arabic RTL text.
    private static string BuildStudentMessage(
        User student,
        AssessmentPublishInfo info,
        string? context,
        string link)
    {
        var builder = new StringBuilder();
        builder.Append($"مرحباً {student.DisplayName}\n\n");
        builder.Append($"تم نشر {info.KindLabel} جديد: {info.Title}\n");
        AppendDetails(builder, info, context);
        builder.Append("\nافتح من هذا الرابط:\n\n");
        builder.Append(link);
        return builder.ToString();
    }

    private static string BuildParentMessage(
        User student,
        AssessmentPublishInfo info,
        string? context,
        string link)
    {
        var builder = new StringBuilder();
        builder.Append("رسالة لولي الأمر\n\n");
        builder.Append($"تم نشر {info.KindLabel} جديد للطالب {student.DisplayName}: {info.Title}\n");
        AppendDetails(builder, info, context);
        builder.Append("\nيمكنك الاطلاع عليه من هذا الرابط:\n\n");
        builder.Append(link);
        return builder.ToString();
    }

    private static string BuildTeacherMessage(
        User teacher,
        AssessmentPublishInfo info,
        string? context,
        string studentLink,
        string teacherLink)
    {
        var builder = new StringBuilder();
        builder.Append($"مرحباً أستاذ/ة {teacher.DisplayName}\n\n");
        builder.Append($"تم نشر {info.KindLabel} جديد: {info.Title}\n");
        AppendDetails(builder, info, context);
        builder.Append("\nرابط الطالب (يمكنك إعادة إرساله):\n\n");
        builder.Append(studentLink);
        builder.Append("\n\nلوحة المعلم:\n\n");
        builder.Append(teacherLink);
        return builder.ToString();
    }

    private static void AppendDetails(StringBuilder builder, AssessmentPublishInfo info, string? context)
    {
        if (!string.IsNullOrWhiteSpace(context))
        {
            builder.Append($"الصف والمادة: {context}\n");
        }

        if (info.DueAtUtc is DateTimeOffset due)
        {
            builder.Append($"آخر موعد للتسليم: {FormatLocal(due)}\n");
        }
    }

    private static readonly TimeZoneInfo DisplayTimeZone = ResolveDisplayTimeZone();

    private static TimeZoneInfo ResolveDisplayTimeZone()
    {
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // Try the next platform-specific id.
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static string FormatLocal(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, DisplayTimeZone).ToString("dd/MM/yyyy HH:mm");

    private static string Truncate(string value) =>
        value.Length <= 1000 ? value : value[..997] + "...";

    private static string Digits(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}
