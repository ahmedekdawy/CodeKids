using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class SubmitAssignmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SubmitAssignmentCommand, AssignmentSubmissionDto>
{
    public async Task<AssignmentSubmissionDto> Handle(SubmitAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Questions)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        if (assignment.Classroom?.Students.All(s => s.StudentId != command.StudentId) == true)
        {
            throw new InvalidOperationException("Student is not in this classroom.");
        }

        if (!assignment.IsPublished)
        {
            throw new InvalidOperationException("Assignment is not available.");
        }

        if (await dbContext.AssignmentSubmissions.AnyAsync(
                x => x.AssignmentId == assignment.Id && x.StudentId == command.StudentId, cancellationToken))
        {
            throw new InvalidOperationException("Assignment already submitted.");
        }

        var student = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.StudentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var submission = new AssignmentSubmission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StudentId = student.Id,
            Status = AssignmentSubmissionStatus.Submitted,
            StartedAtUtc = DateTimeOffset.UtcNow,
            SubmittedAtUtc = DateTimeOffset.UtcNow
        };

        var autoScore = 0;
        var answerable = assignment.Questions
            .Where(x => x.QuestionType != AssignmentQuestionType.Paragraph)
            .ToList();
        var maxScore = answerable.Sum(x => x.Points);
        var allAutoGradable = true;

        foreach (var question in answerable)
        {
            var input = command.Answers.FirstOrDefault(x => x.QuestionId == question.Id);
            var answerText = (input?.AnswerText ?? string.Empty).Trim();
            var answerImageId = input?.AnswerImageMediaAssetId;
            await QuestionImageAssetValidator.EnsureExistsAsync(dbContext, answerImageId, cancellationToken);
            bool? isCorrect = null;
            int? points = null;

            if (answerImageId is not null)
            {
                allAutoGradable = false;
            }

            if (TypedQuestionSupport.IsTeacherGradedText(question.QuestionType))
            {
                allAutoGradable = false;
                if (TypedQuestionSupport.IsShortAnswer(question.QuestionType)
                    && !string.IsNullOrWhiteSpace(question.CorrectAnswer)
                    && string.Equals(answerText, question.CorrectAnswer, StringComparison.OrdinalIgnoreCase)
                    && answerImageId is null)
                {
                    isCorrect = true;
                    points = question.Points;
                    autoScore += question.Points;
                }
            }
            else if (answerImageId is null)
            {
                var bankType = TypedQuestionSupport.ToBankType(question.QuestionType);
                if (question.QuestionType == AssignmentQuestionType.MultiChoice)
                {
                    answerText = string.Join(',', ExamGrading.NormalizeMultiAnswer(answerText));
                }

                if (ExamGrading.IsAutoGradable(bankType))
                {
                    isCorrect = ExamGrading.AnswersMatch(bankType, answerText, question.CorrectAnswer);
                    points = isCorrect == true ? question.Points : 0;
                    if (isCorrect == true) autoScore += question.Points;
                }
                else
                {
                    allAutoGradable = false;
                }
            }

            submission.Answers.Add(new AssignmentAnswer
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                QuestionId = question.Id,
                AnswerText = answerText,
                AnswerImageMediaAssetId = answerImageId,
                IsCorrect = isCorrect,
                PointsAwarded = points
            });
        }

        submission.MaxScore = maxScore;
        if (allAutoGradable)
        {
            submission.Score = autoScore;
            submission.Status = AssignmentSubmissionStatus.Graded;
            submission.GradedAtUtc = DateTimeOffset.UtcNow;
            if (autoScore >= Math.Ceiling(maxScore * 0.7))
            {
                student.TotalXp += assignment.XpReward;
            }
        }

        dbContext.AssignmentSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);
        await BadgeAwarder.AwardEligibleAsync(dbContext, student, cancellationToken);

        return (await LoadSubmission(dbContext, submission.Id, cancellationToken))!;
    }

    internal static async Task<AssignmentSubmissionDto?> LoadSubmission(
        IAppDbContext dbContext,
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await dbContext.AssignmentSubmissions
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Assignment)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

        return submission is null ? null : MapSubmission(submission);
    }

    internal static AssignmentSubmissionDto MapSubmission(AssignmentSubmission submission) =>
        new(
            submission.Id,
            submission.AssignmentId,
            submission.Assignment?.Title ?? "Assignment",
            submission.StudentId,
            submission.Student?.DisplayName ?? "Student",
            submission.Status.ToString(),
            submission.Score,
            submission.MaxScore,
            submission.TeacherFeedback,
            QuestionImageUrls.Build(submission.FeedbackImageMediaAssetId),
            submission.StartedAtUtc,
            submission.SubmittedAtUtc,
            submission.GradedAtUtc,
            submission.Assignment?.SolutionVideoMediaAssetId,
            submission.Answers.Select(a => new AssignmentAnswerReviewDto(
                a.QuestionId,
                a.Question?.Prompt ?? "",
                a.AnswerText,
                a.Question?.CorrectAnswer,
                a.IsCorrect,
                a.PointsAwarded,
                a.Question?.Points ?? 0,
                QuestionImageUrls.Build(a.Question?.PromptImageMediaAssetId),
                QuestionImageUrls.Build(a.AnswerImageMediaAssetId))).ToList());
}
