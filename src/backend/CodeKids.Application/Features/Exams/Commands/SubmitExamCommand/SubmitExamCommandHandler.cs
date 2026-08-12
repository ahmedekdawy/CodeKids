using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
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
            if (question.QuestionType == BankQuestionType.MultiChoice)
            {
                answerText = string.Join(',', ExamGrading.NormalizeMultiAnswer(answerText));
            }

            var isCorrect = ExamGrading.AnswersMatch(question.QuestionType, answerText, question.CorrectAnswer);
            var points = isCorrect ? question.Points : 0;
            if (isCorrect) autoScore += question.Points;

            attempt.Answers.Add(new ExamAnswer
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                ExamQuestionId = question.Id,
                AnswerText = answerText,
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
                a.Question?.Points ?? 0)).ToList());
}
