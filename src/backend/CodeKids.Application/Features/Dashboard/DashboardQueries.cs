using CodeKids.Application.Features.Analytics;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Dashboard;

public sealed record ChildProgressDto(
    Guid StudentId,
    string DisplayName,
    int TotalXp,
    int CompletedSteps,
    int QuizAttempts,
    Guid? AvatarId,
    IReadOnlyList<string> Badges);

public sealed record ParentDashboardDto(
    Guid ParentId,
    string ParentName,
    IReadOnlyList<ChildProgressDto> Children);

public sealed record TeacherStudentDto(
    Guid StudentId,
    string DisplayName,
    string Email,
    int TotalXp,
    int LevelNumber,
    string LevelName,
    int LevelProgressPercent,
    int CompletedSteps,
    int QuizAttempts,
    int WeakLessonCount,
    string? ParentName,
    string? Signal);

public sealed record TeacherDashboardDto(
    Guid TeacherId,
    string TeacherName,
    int StudentCount,
    int TotalCompletedSteps,
    int AverageXp,
    int BehindCount,
    IReadOnlyList<string> TopWeakLessons,
    IReadOnlyList<TeacherStudentDto> Students);

public sealed record GetParentDashboardQuery(Guid ParentId) : IQuery<ParentDashboardDto>;

public sealed record GetTeacherDashboardQuery(Guid TeacherId) : IQuery<TeacherDashboardDto>;

public sealed class GetParentDashboardQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetParentDashboardQuery, ParentDashboardDto>
{
    public async Task<ParentDashboardDto> Handle(GetParentDashboardQuery query, CancellationToken cancellationToken)
    {
        var parent = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ParentId && x.Role == UserRole.Parent, cancellationToken)
            ?? throw new InvalidOperationException("Parent account not found.");

        var children = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Badges)
                .ThenInclude(x => x.Badge)
            .Where(x => x.ParentId == parent.Id && x.Role == UserRole.Student)
            .ToListAsync(cancellationToken);

        var childIds = children.Select(x => x.Id).ToList();
        var progressCounts = await dbContext.StudentProgress
            .Where(x => childIds.Contains(x.UserId) && x.IsCompleted)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var quizCounts = await dbContext.QuizAttempts
            .Where(x => childIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        return new ParentDashboardDto(
            parent.Id,
            parent.DisplayName,
            children.Select(child => new ChildProgressDto(
                child.Id,
                child.DisplayName,
                child.TotalXp,
                progressCounts.GetValueOrDefault(child.Id),
                quizCounts.GetValueOrDefault(child.Id),
                child.AvatarId,
                child.Badges.Select(x => x.Badge!.Name).ToList())).ToList());
    }
}

public sealed class GetTeacherDashboardQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetTeacherDashboardQuery, TeacherDashboardDto>
{
    public async Task<TeacherDashboardDto> Handle(GetTeacherDashboardQuery query, CancellationToken cancellationToken)
    {
        var teacher = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.TeacherId && x.Role == UserRole.Teacher, cancellationToken)
            ?? throw new InvalidOperationException("Teacher account not found.");

        var studentIds = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => x.Classroom!.Courses.Any(t => t.TeacherId == query.TeacherId))
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var students = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Parent)
            .Where(x => studentIds.Contains(x.Id) && x.Role == UserRole.Student)
            .OrderByDescending(x => x.TotalXp)
            .ToListAsync(cancellationToken);

        var progressCounts = await dbContext.StudentProgress
            .Where(x => studentIds.Contains(x.UserId) && x.IsCompleted)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var quizCounts = await dbContext.QuizAttempts
            .Where(x => studentIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var classroomWeak = await AnalyticsQueries.GetWeakLessonsForClassroom(
            dbContext, studentIds, cancellationToken);

        var mapped = new List<TeacherStudentDto>();
        var avgXp = students.Count == 0 ? 0 : students.Average(x => x.TotalXp);
        var behindCount = 0;

        foreach (var student in students)
        {
            var level = StudentLevelCalculator.FromXp(student.TotalXp);
            var weak = await AnalyticsQueries.GetWeakLessonsForStudent(dbContext, student.Id, cancellationToken);
            string? signal = null;
            if (student.TotalXp < avgXp * 0.6 || level.LevelNumber <= 1)
            {
                signal = "Behind";
                behindCount++;
            }
            else if (weak.Count > 0 && weak[0].AccuracyPercent < 50)
            {
                signal = "Needs review";
            }
            else if (level.LevelNumber >= 4)
            {
                signal = "Strong";
            }

            mapped.Add(new TeacherStudentDto(
                student.Id,
                student.DisplayName,
                student.Email,
                student.TotalXp,
                level.LevelNumber,
                level.Name,
                level.ProgressPercent,
                progressCounts.GetValueOrDefault(student.Id),
                quizCounts.GetValueOrDefault(student.Id),
                weak.Count,
                student.Parent?.DisplayName,
                signal));
        }

        return new TeacherDashboardDto(
            teacher.Id,
            teacher.DisplayName,
            mapped.Count,
            mapped.Sum(x => x.CompletedSteps),
            mapped.Count == 0 ? 0 : (int)mapped.Average(x => x.TotalXp),
            behindCount,
            classroomWeak.Take(5).Select(x => x.LessonTitle).ToList(),
            mapped);
    }
}
