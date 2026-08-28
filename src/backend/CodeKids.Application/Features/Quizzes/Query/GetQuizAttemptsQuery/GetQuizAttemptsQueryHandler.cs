using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class GetQuizAttemptsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetQuizAttemptsQuery, IReadOnlyList<QuizAttemptReviewDto>>
{
    public async Task<IReadOnlyList<QuizAttemptReviewDto>> Handle(
        GetQuizAttemptsQuery query,
        CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .AsNoTracking()
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .FirstOrDefaultAsync(x => x.Id == query.QuizId, cancellationToken)
            ?? throw new InvalidOperationException("Quiz not found.");

        var isCreator = quiz.CreatedByUserId == query.TeacherUserId;
        var isClassroomTeacher = quiz.Classroom?.Courses.Any(t => t.TeacherId == query.TeacherUserId) == true;
        if (!isCreator && !isClassroomTeacher)
        {
            throw new InvalidOperationException("Only the quiz teacher can review attempts.");
        }

        var attempts = await dbContext.QuizAttempts
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .Where(x => x.QuizId == query.QuizId)
            .OrderByDescending(x => x.CompletedAtUtc)
            .ToListAsync(cancellationToken);

        return attempts.Select(MapAttempt).ToList();
    }

    private static QuizAttemptReviewDto MapAttempt(Domain.Entities.QuizAttempt attempt)
    {
        var answers = attempt.Answers
            .OrderBy(a => a.Question?.SortOrder ?? 0)
            .Select(a =>
            {
                var options = ChoiceOptions.Parse(
                    a.Question?.OptionsJson,
                    a.Question?.OptionA,
                    a.Question?.OptionB,
                    a.Question?.OptionC);
                var selected = a.SelectedOption ?? string.Empty;
                var correct = a.Question?.CorrectOption ?? string.Empty;
                return new QuizAnswerReviewDto(
                    a.QuestionId,
                    a.Question?.Prompt ?? string.Empty,
                    a.Question?.SortOrder ?? 0,
                    selected,
                    OptionText(options, selected),
                    correct,
                    OptionText(options, correct),
                    a.IsCorrect,
                    QuestionImageUrls.Build(a.Question?.PromptImageMediaAssetId));
            })
            .ToList();

        return new QuizAttemptReviewDto(
            attempt.Id,
            attempt.QuizId,
            attempt.UserId,
            attempt.User?.DisplayName ?? "Student",
            attempt.Score,
            attempt.TotalQuestions,
            attempt.EarnedXp,
            attempt.CompletedAtUtc,
            answers);
    }

    private static string OptionText(IReadOnlyList<ChoiceOptionDto> options, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var match = options.FirstOrDefault(o => string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase));
        return match is null ? key : $"{match.Key}) {match.Text}";
    }
}
