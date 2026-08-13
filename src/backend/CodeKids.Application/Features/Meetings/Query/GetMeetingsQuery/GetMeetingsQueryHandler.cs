using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Meetings;

public sealed class GetMeetingsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetMeetingsQuery, IReadOnlyList<LiveSessionDto>>
{
    public async Task<IReadOnlyList<LiveSessionDto>> Handle(GetMeetingsQuery query, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-30);
        var sessions = await dbContext.LiveSessions
            .AsNoTracking()
            .Include(x => x.Host)
            .Include(x => x.Course)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .Where(x => x.StartsAtUtc >= now)
            .OrderBy(x => x.StartsAtUtc)
            .ToListAsync(cancellationToken);

        var isTeacher = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase);
        var isStudent = string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);
        var isParent = string.Equals(query.ViewerRole, nameof(UserRole.Parent), StringComparison.OrdinalIgnoreCase);
        var isAdmin = string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);

        if (isTeacher)
        {
            sessions = sessions.Where(x => x.HostUserId == query.ViewerUserId || x.Classroom?.Courses.Any(t => t.TeacherId == query.ViewerUserId) == true).ToList();
        }
        else if (isStudent)
        {
            var visibleCourseIds = await StudentCourseVisibility.GetVisibleCourseIdsAsync(
                dbContext, query.ViewerUserId, cancellationToken);

            sessions = sessions
                .Where(x => x.ClassroomId is null || x.Classroom!.Students.Any(s => s.StudentId == query.ViewerUserId))
                .Where(x =>
                    x.ClassroomId is null
                    || x.CourseId is null
                    || visibleCourseIds.Contains(x.CourseId.Value))
                .ToList();
        }
        else if (isParent)
        {
            var childIds = await dbContext.Users
                .Where(x => x.ParentId == query.ViewerUserId && x.Role == UserRole.Student)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            sessions = sessions
                .Where(x => x.ClassroomId is null || x.Classroom!.Students.Any(s => childIds.Contains(s.StudentId)))
                .ToList();
        }
        else if (!isAdmin)
        {
            sessions = [];
        }

        return sessions.Select(session => new LiveSessionDto(
            session.Id,
            session.Title,
            session.Description,
            session.HostUserId,
            session.Host?.DisplayName ?? "Teacher",
            session.CourseId,
            session.Course?.Title,
            session.ClassroomId,
            session.Classroom?.Name,
            session.StartsAtUtc,
            session.DurationMinutes,
            session.JoinUrl,
            isTeacher && session.HostUserId == query.ViewerUserId ? session.StartUrl : null,
            session.WhatsAppNotified,
            null,
            null)).ToList();
    }
}
