using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed class SubmitExamCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SubmitExamCommand, ExamAttemptDto>
{
    public async Task<ExamAttemptDto> Handle(SubmitExamCommand command, CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .Include(x => x.Questions)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .FirstOrDefaultAsync(x => x.Id == command.ExamId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found.");

        if (exam.Classroom?.Students.All(s => s.StudentId != command.StudentId) == true)
        {
            throw new InvalidOperationException("Student is not in this classroom.");
        }

        if (!exam.IsPublished)
        {
            throw new InvalidOperationException("Exam is not available.");
        }

        var student = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.StudentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var attempt = await dbContext.ExamAttempts
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(
                x => x.ExamId == exam.Id && x.StudentId == command.StudentId,
                cancellationToken);

        if (attempt is not null && attempt.Status != ExamAttemptStatus.InProgress)
        {
            throw new InvalidOperationException("Exam already submitted.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (attempt is null)
        {
            attempt = new ExamAttempt
            {
                Id = Guid.NewGuid(),
                ExamId = exam.Id,
                StudentId = student.Id,
                Status = ExamAttemptStatus.InProgress,
                StartedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.ExamAttempts.Add(attempt);
        }
        else
        {
            // Claim the attempt in a single statement so only one of two overlapping
            // submissions (submit button racing the expiry timer, or a retried request)
            // goes on to replace the answer rows below.
            var claimed = await dbContext.ExamAttempts
                .Where(x => x.Id == attempt.Id && x.Status == ExamAttemptStatus.InProgress)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(a => a.Status, ExamAttemptStatus.Submitted),
                    cancellationToken);
            if (claimed == 0)
            {
                throw new InvalidOperationException("Exam already submitted.");
            }
        }

        // Delete through the set rather than by emptying the navigation: answers added to a
        // navigation on an already-persisted attempt are tracked as Modified (their keys are
        // pre-assigned), which makes EF issue UPDATEs for rows that were never inserted.
        dbContext.ExamAnswers.RemoveRange(attempt.Answers);
        attempt.Answers.Clear();
        attempt.Status = ExamAttemptStatus.Submitted;
        attempt.SubmittedAtUtc = DateTimeOffset.UtcNow;

        var autoScore = 0;
        var answerable = exam.Questions
            .Where(x => !BankQuestionValidator.IsComposite(x.QuestionType))
            .ToList();
        var maxScore = answerable.Sum(x => x.Points);
        var allAutoGradable = answerable.All(x => ExamGrading.IsAutoGradable(x.QuestionType));

        foreach (var question in answerable)
        {
            var input = command.Answers.FirstOrDefault(x => x.QuestionId == question.Id);
            var answerText = (input?.AnswerText ?? string.Empty).Trim();
            var answerImageId = input?.AnswerImageMediaAssetId;
            await QuestionImageAssetValidator.EnsureExistsAsync(dbContext, answerImageId, cancellationToken);
            if (question.QuestionType == BankQuestionType.MultiChoice)
            {
                answerText = string.Join(',', ExamGrading.NormalizeMultiAnswer(answerText));
            }

            if (answerImageId is not null || !ExamGrading.IsAutoGradable(question.QuestionType))
            {
                allAutoGradable = false;
                dbContext.ExamAnswers.Add(new ExamAnswer
                {
                    Id = Guid.NewGuid(),
                    AttemptId = attempt.Id,
                    ExamQuestionId = question.Id,
                    AnswerText = answerText,
                    AnswerImageMediaAssetId = answerImageId,
                    IsCorrect = null,
                    PointsAwarded = null
                });
                continue;
            }

            var isCorrect = ExamGrading.AnswersMatch(question.QuestionType, answerText, question.CorrectAnswer);
            var points = isCorrect ? question.Points : 0;
            if (isCorrect) autoScore += question.Points;

            dbContext.ExamAnswers.Add(new ExamAnswer
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                ExamQuestionId = question.Id,
                AnswerText = answerText,
                AnswerImageMediaAssetId = answerImageId,
                IsCorrect = isCorrect,
                PointsAwarded = points
            });
        }

        attempt.MaxScore = maxScore;
        if (allAutoGradable)
        {
            attempt.Score = autoScore;
            attempt.Status = ExamAttemptStatus.Graded;
            attempt.GradedAtUtc = DateTimeOffset.UtcNow;
            if (maxScore > 0 && autoScore >= Math.Ceiling(maxScore * 0.7))
            {
                student.TotalXp += exam.XpReward;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await BadgeAwarder.AwardEligibleAsync(dbContext, student, cancellationToken);
        return (await LoadAttempt(dbContext, attempt.Id, cancellationToken))!;
    }

    internal static async Task<ExamAttemptDto?> LoadAttempt(
        IAppDbContext dbContext,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await dbContext.ExamAttempts
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Exam)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(x => x.Id == attemptId, cancellationToken);
        return attempt is null ? null : MapAttempt(attempt);
    }

    internal static ExamAttemptDto MapAttempt(ExamAttempt attempt) =>
        new(
            attempt.Id,
            attempt.ExamId,
            attempt.Exam?.Title ?? "Exam",
            attempt.StudentId,
            attempt.Student?.DisplayName ?? "Student",
            attempt.Status.ToString(),
            attempt.Score,
            attempt.MaxScore,
            attempt.TeacherFeedback,
            QuestionImageUrls.Build(attempt.FeedbackImageMediaAssetId),
            attempt.StartedAtUtc,
            attempt.SubmittedAtUtc,
            attempt.GradedAtUtc,
            attempt.DurationSeconds,
            attempt.Answers.Select(a => new ExamAnswerReviewDto(
                a.ExamQuestionId,
                a.Question?.Prompt ?? "",
                a.Question?.QuestionType.ToString() ?? "",
                a.AnswerText,
                a.Question?.CorrectAnswer,
                a.IsCorrect,
                a.PointsAwarded,
                a.Question?.Points ?? 0,
                QuestionImageUrls.Build(a.Question?.PromptImageMediaAssetId),
                QuestionImageUrls.Build(a.AnswerImageMediaAssetId))).ToList());
}
