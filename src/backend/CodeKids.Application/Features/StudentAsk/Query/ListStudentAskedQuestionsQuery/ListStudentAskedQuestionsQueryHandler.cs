using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAsk;

public sealed class ListStudentAskedQuestionsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListStudentAskedQuestionsQuery, IReadOnlyList<StudentAskedQuestionDto>>
{
    public async Task<IReadOnlyList<StudentAskedQuestionDto>> Handle(
        ListStudentAskedQuestionsQuery query,
        CancellationToken cancellationToken)
    {
        var allowed = await GetAllowedCourseIdsAsync(query.ViewerId, query.ViewerRole, cancellationToken);
        if (allowed is { Count: 0 })
        {
            return [];
        }

        var rows = dbContext.StudentAskedQuestions.AsNoTracking();
        if (allowed is not null)
        {
            rows = rows.Where(x => allowed.Contains(x.CourseId));
        }

        if (query.CourseId is Guid courseId)
        {
            rows = rows.Where(x => x.CourseId == courseId);
        }

        if (query.UnitId is Guid unitId)
        {
            rows = rows.Where(x => x.UnitId == unitId);
        }

        if (query.LessonId is Guid lessonId)
        {
            rows = rows.Where(x => x.LessonId == lessonId);
        }

        if (query.FromDate is DateOnly from)
        {
            var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            rows = rows.Where(x => x.CreatedAtUtc >= fromUtc);
        }

        if (query.ToDate is DateOnly to)
        {
            var toExclusive = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            rows = rows.Where(x => x.CreatedAtUtc < toExclusive);
        }

        var search = (query.QuestionText ?? string.Empty).Trim().ToLowerInvariant();
        if (search.Length > 0)
        {
            rows = rows.Where(x => x.Question.ToLower().Contains(search));
        }

        var isStudent = string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);
        if (isStudent)
        {
            rows = rows.Where(x => x.AiInScope || x.StudentId == query.ViewerId);
        }

        var list = await rows
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.StudentId,
                x.StudentName,
                x.CourseId,
                x.CourseTitle,
                x.UnitId,
                x.UnitTitle,
                x.LessonId,
                x.LessonTitle,
                x.Question,
                x.AiAnswer,
                x.AiInScope,
                x.TeacherAnswer,
                TeacherName = x.Teacher != null ? x.Teacher.DisplayName : string.Empty,
                x.CreatedAtUtc,
                x.TeacherAnsweredAtUtc
            })
            .ToListAsync(cancellationToken);

        return list
            .Select(x => new StudentAskedQuestionDto(
                x.Id,
                x.StudentId,
                x.StudentName,
                x.CourseId,
                x.CourseTitle,
                x.UnitId,
                x.UnitTitle,
                x.LessonId,
                x.LessonTitle,
                x.Question,
                x.AiAnswer,
                x.AiInScope,
                x.TeacherAnswer,
                x.TeacherName,
                x.CreatedAtUtc,
                x.TeacherAnsweredAtUtc,
                x.StudentId == query.ViewerId))
            .ToList();
    }

    private async Task<HashSet<Guid>?> GetAllowedCourseIdsAsync(
        Guid viewerId,
        string? role,
        CancellationToken cancellationToken)
    {
        if (string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(role, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase))
        {
            var ids = await dbContext.ClassroomCourses
                .AsNoTracking()
                .Where(x => x.TeacherId == viewerId)
                .Select(x => x.CourseId)
                .Distinct()
                .ToListAsync(cancellationToken);
            return ids.ToHashSet();
        }

        if (string.Equals(role, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase))
        {
            return await StudentCourseVisibility.GetVisibleCourseIdsAsync(dbContext, viewerId, cancellationToken);
        }

        return [];
    }
}
