using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Meetings;

public sealed class CreateMeetingCommandHandler(
    IAppDbContext dbContext,
    IZoomMeetingClient zoomMeetingClient,
    IZoomUserOAuthService zoomUserOAuth,
    IWhatsAppClient whatsAppClient) : ICommandHandler<CreateMeetingCommand, LiveSessionDto>
{
    public async Task<LiveSessionDto> Handle(CreateMeetingCommand command, CancellationToken cancellationToken)
    {
        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (command.DurationMinutes is < 15 or > 240)
        {
            throw new InvalidOperationException("Duration must be between 15 and 240 minutes.");
        }

        if (command.StartsAtUtc < DateTimeOffset.UtcNow.AddMinutes(-5))
        {
            throw new InvalidOperationException("Start time must be in the future.");
        }

        var host = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == command.HostUserId && x.Role == UserRole.Teacher, cancellationToken)
            ?? throw new InvalidOperationException("Teacher account not found.");

        var classroom = await dbContext.Classrooms
            .Include(x => x.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        if (!classroom.Courses.Any(t => t.TeacherId == host.Id))
        {
            throw new InvalidOperationException("You can only schedule Zoom meetings for classrooms assigned to you.");
        }

        var courseId = command.CourseId ?? classroom.CourseId;
        string? courseTitle = null;
        if (courseId is Guid cid)
        {
            var course = await dbContext.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == cid, cancellationToken)
                ?? throw new InvalidOperationException("Course not found.");
            courseTitle = course.Title;
        }

        var description = (command.Description ?? string.Empty).Trim();
        var userAccessToken = await EnsurePersonalZoomAccessTokenAsync(host, cancellationToken);
        var zoomMeeting = await zoomMeetingClient.CreateMeetingAsync(
            new ZoomMeetingRequest(title, description, command.StartsAtUtc, command.DurationMinutes, userAccessToken),
            cancellationToken);

        var message =
            $"CodeKids live class: {title}\n" +
            $"When: {command.StartsAtUtc.ToUniversalTime():u}\n" +
            $"Duration: {command.DurationMinutes} min\n" +
            $"Join Zoom: {zoomMeeting.JoinUrl}";

        if (!string.IsNullOrWhiteSpace(classroom.WhatsAppGroupInviteUrl))
        {
            message += $"\nClass WhatsApp group: {classroom.WhatsAppGroupInviteUrl}";
        }

        var shareUrl = whatsAppClient.BuildShareUrl(message);
        var whatsAppStatus = "Not requested";
        var notified = false;

        if (command.NotifyWhatsApp)
        {
            var phones = classroom.WhatsAppNotifyPhones
                .Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var studentPhones = await dbContext.ClassroomStudents
                .AsNoTracking()
                .Where(x => x.ClassroomId == classroom.Id)
                .Join(dbContext.Users.AsNoTracking(), cs => cs.StudentId, u => u.Id, (_, u) => u.MobilePhone)
                .Where(phone => phone != null && phone != "")
                .ToListAsync(cancellationToken);

            phones.AddRange(studentPhones.Select(p => p.Trim()).Where(p => p.Length > 0));
            phones = phones.Distinct(StringComparer.Ordinal).ToList();

            if (phones.Count == 0)
            {
                whatsAppStatus = $"No student mobiles or notify phones configured. Use share link: {shareUrl}";
            }
            else
            {
                var details = new List<string>();
                foreach (var phone in phones)
                {
                    var result = await whatsAppClient.SendTextAsync(phone, message, cancellationToken);
                    details.Add($"{phone}: {result.Detail}");
                    if (result.Sent) notified = true;
                }

                whatsAppStatus = string.Join(" | ", details);
            }
        }

        var session = new LiveSession
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            HostUserId = host.Id,
            CourseId = courseId,
            ClassroomId = classroom.Id,
            StartsAtUtc = command.StartsAtUtc.ToUniversalTime(),
            DurationMinutes = command.DurationMinutes,
            ZoomMeetingId = zoomMeeting.MeetingId,
            JoinUrl = zoomMeeting.JoinUrl,
            StartUrl = zoomMeeting.StartUrl,
            WhatsAppNotified = notified,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.LiveSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LiveSessionDto(
            session.Id,
            session.Title,
            session.Description,
            session.HostUserId,
            host.DisplayName,
            session.CourseId,
            courseTitle,
            session.ClassroomId,
            classroom.Name,
            session.StartsAtUtc,
            session.DurationMinutes,
            session.JoinUrl,
            session.StartUrl,
            session.WhatsAppNotified,
            shareUrl,
            whatsAppStatus);
    }

    private async Task<string?> EnsurePersonalZoomAccessTokenAsync(User host, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host.ZoomRefreshToken))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(host.ZoomAccessToken)
            && host.ZoomTokenExpiresAt is DateTimeOffset expires
            && expires > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return host.ZoomAccessToken;
        }

        if (!zoomUserOAuth.IsUserOAuthConfigured)
        {
            return string.IsNullOrWhiteSpace(host.ZoomAccessToken) ? null : host.ZoomAccessToken;
        }

        var tokens = await zoomUserOAuth.RefreshAsync(host.ZoomRefreshToken, cancellationToken);
        host.ZoomAccessToken = tokens.AccessToken;
        host.ZoomRefreshToken = tokens.RefreshToken;
        host.ZoomTokenExpiresAt = tokens.ExpiresAt;
        if (!string.IsNullOrWhiteSpace(tokens.Email))
        {
            host.ZoomConnectedEmail = tokens.Email;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return host.ZoomAccessToken;
    }
}
