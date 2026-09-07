using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.Notifications;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed class GradeExamAttemptCommandHandler(IAppDbContext dbContext, NotificationPublisher notifications)
    : ICommandHandler<GradeExamAttemptCommand, ExamAttemptDto>
{
    public async Task<ExamAttemptDto> Handle(GradeExamAttemptCommand command, CancellationToken cancellationToken)
    {
        var attempt = await dbContext.ExamAttempts
            .Include(x => x.Answers)
            .Include(x => x.Exam!)
                .ThenInclude(e => e.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Exam!)
                .ThenInclude(e => e.Questions)
            .FirstOrDefaultAsync(x => x.Id == command.AttemptId, cancellationToken)
            ?? throw new InvalidOperationException("Exam attempt not found.");

        if (attempt.Exam?.Classroom?.Courses.Any(t => t.TeacherId == command.TeacherUserId) != true)
        {
            throw new InvalidOperationException("Only the classroom teacher can grade exam attempts.");
        }

        if (command.Answers is not null)
        {
            foreach (var grade in command.Answers)
            {
                var answer = attempt.Answers.FirstOrDefault(x => x.ExamQuestionId == grade.QuestionId);
                if (answer is null) continue;
                answer.IsCorrect = grade.IsCorrect;
                answer.PointsAwarded = Math.Max(0, grade.PointsAwarded);
            }
        }

        var wasAlreadyGraded = attempt.Status == ExamAttemptStatus.Graded;

        attempt.Score = attempt.Answers.Sum(x => x.PointsAwarded ?? 0);
        attempt.MaxScore = attempt.Exam!.Questions
            .Where(x => !BankQuestionValidator.IsComposite(x.QuestionType))
            .Sum(x => x.Points);
        attempt.TeacherFeedback = command.TeacherFeedback?.Trim();
        await QuestionImageAssetValidator.EnsureExistsAsync(dbContext, command.FeedbackImageMediaAssetId, cancellationToken);
        attempt.FeedbackImageMediaAssetId = command.FeedbackImageMediaAssetId;
        attempt.Status = ExamAttemptStatus.Graded;
        attempt.GradedAtUtc = DateTimeOffset.UtcNow;

        var student = await dbContext.Users.FirstAsync(x => x.Id == attempt.StudentId, cancellationToken);
        if (!wasAlreadyGraded &&
            attempt.MaxScore > 0 &&
            attempt.Score >= Math.Ceiling(attempt.MaxScore.Value * 0.7))
        {
            student.TotalXp += attempt.Exam.XpReward;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BadgeAwarder.AwardEligibleAsync(dbContext, student, cancellationToken);
        await notifications.NotifyExamGradedAsync(attempt, cancellationToken);

        return (await SubmitExamCommandHandler.LoadAttempt(dbContext, attempt.Id, cancellationToken))!;
    }
}
