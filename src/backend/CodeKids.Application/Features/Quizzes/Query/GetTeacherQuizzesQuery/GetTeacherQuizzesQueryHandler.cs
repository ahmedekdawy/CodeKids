using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class GetTeacherQuizzesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetTeacherQuizzesQuery, IReadOnlyList<TeacherQuizListDto>>
{
    public async Task<IReadOnlyList<TeacherQuizListDto>> Handle(
        GetTeacherQuizzesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.FromDate is DateOnly fromDate && query.ToDate is DateOnly toDate && toDate < fromDate)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }

        var quizzesQuery = dbContext.Quizzes
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Classroom)
            .Include(x => x.Questions)
            .Where(x => x.CreatedByUserId == query.TeacherUserId);

        if (query.FromDate is DateOnly from)
        {
            var fromUtc = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            quizzesQuery = quizzesQuery.Where(x => x.CreatedAtUtc >= fromUtc);
        }

        if (query.ToDate is DateOnly to)
        {
            var toExclusive = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            quizzesQuery = quizzesQuery.Where(x => x.CreatedAtUtc < toExclusive);
        }

        if (query.Grade is int grade)
        {
            quizzesQuery = quizzesQuery.Where(x =>
                x.Course != null
                && (x.Course.Grade == grade
                    || (x.Course.Grade == null && x.Course.StageId == null)
                    || (x.Course.Grade == null
                        && x.Course.StageId != null
                        && dbContext.Grades.Any(g => g.Id == grade && g.StageId == x.Course.StageId))));
        }

        if (query.CourseId is Guid courseId)
        {
            quizzesQuery = quizzesQuery.Where(x => x.CourseId == courseId);
        }

        var quizzes = await quizzesQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var quizIds = quizzes.Select(x => x.Id).ToList();
        var attemptCounts = quizIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await dbContext.QuizAttempts
                .AsNoTracking()
                .Where(x => quizIds.Contains(x.QuizId))
                .GroupBy(x => x.QuizId)
                .Select(g => new { QuizId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.QuizId, x => x.Count, cancellationToken);

        return quizzes.Select(quiz => new TeacherQuizListDto(
            quiz.Id,
            quiz.CourseId,
            quiz.Course?.Title ?? string.Empty,
            quiz.Course?.Grade,
            quiz.ClassroomId,
            quiz.Classroom?.Name,
            quiz.Title,
            quiz.Description,
            quiz.XpReward,
            quiz.Questions.Count,
            attemptCounts.GetValueOrDefault(quiz.Id),
            quiz.CreatedAtUtc)).ToList();
    }
}
