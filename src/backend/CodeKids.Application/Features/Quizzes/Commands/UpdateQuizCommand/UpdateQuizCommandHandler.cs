using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Application.Features.Notifications;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class UpdateQuizCommandHandler(IAppDbContext dbContext, NotificationPublisher notifications)
    : ICommandHandler<UpdateQuizCommand, QuizDto>
{
    public async Task<QuizDto> Handle(UpdateQuizCommand command, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .Include(x => x.Questions)
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.QuizId, cancellationToken)
            ?? throw new InvalidOperationException("Quiz not found.");

        QuizAuthorization.EnsureCanManage(quiz, command.TeacherUserId);

        var courseExists = await dbContext.Courses.AnyAsync(x => x.Id == command.CourseId, cancellationToken);
        if (!courseExists)
        {
            throw new InvalidOperationException("Course not found.");
        }

        if (command.ClassroomId is Guid classroomId)
        {
            var classroom = await dbContext.Classrooms
                .Include(x => x.Courses)
                .FirstOrDefaultAsync(x => x.Id == classroomId, cancellationToken)
                ?? throw new InvalidOperationException("Classroom not found.");
            if (!classroom.Courses.Any(t => t.TeacherId == command.TeacherUserId))
            {
                throw new InvalidOperationException("Only an assigned classroom teacher can update quizzes for that classroom.");
            }
        }

        if (command.Questions is null || command.Questions.Count == 0)
        {
            throw new InvalidOperationException("Add at least one question.");
        }

        var title = (command.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Quiz title is required.");
        }

        quiz.CourseId = command.CourseId;
        quiz.ClassroomId = command.ClassroomId;
        quiz.Title = title;
        quiz.Description = (command.Description ?? string.Empty).Trim();
        quiz.XpReward = Math.Max(5, command.XpReward);
        var wasPublished = quiz.IsPublished;
        quiz.IsPublished = command.IsPublished;

        var incoming = command.Questions.ToList();
        var keptIds = incoming
            .Where(q => q.Id is Guid id && quiz.Questions.Any(existing => existing.Id == id))
            .Select(q => q.Id!.Value)
            .ToHashSet();

        var removed = quiz.Questions.Where(q => !keptIds.Contains(q.Id)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(q => q.Id).ToHashSet();
            var answers = await dbContext.QuizAttemptAnswers
                .Where(a => removedIds.Contains(a.QuestionId))
                .ToListAsync(cancellationToken);
            dbContext.QuizAttemptAnswers.RemoveRange(answers);
            dbContext.QuizQuestions.RemoveRange(removed);
        }

        var order = 1;
        foreach (var q in incoming)
        {
            var options = q.Options is { Count: > 0 }
                ? ChoiceOptions.FromTexts(q.Options)
                : ChoiceOptions.Parse(null, q.OptionA, q.OptionB, q.OptionC);

            if (options.Count < 2)
            {
                throw new InvalidOperationException("Each quiz question needs at least two options.");
            }

            var correct = q.CorrectOption.Trim().ToUpperInvariant();
            if (!ChoiceOptions.AllowedKeys(options).Contains(correct))
            {
                throw new InvalidOperationException("Correct option must match one of the listed choices.");
            }

            var (a, b, c, _) = ChoiceOptions.ToLegacy(options);
            await QuestionImageAssetValidator.EnsureExistsAsync(dbContext, q.PromptImageMediaAssetId, cancellationToken);

            var existing = q.Id is Guid id && keptIds.Contains(id)
                ? quiz.Questions.FirstOrDefault(x => x.Id == id)
                : null;
            if (existing is null)
            {
                quiz.Questions.Add(new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    QuizId = quiz.Id,
                    Prompt = q.Prompt.Trim(),
                    OptionA = a ?? string.Empty,
                    OptionB = b ?? string.Empty,
                    OptionC = c ?? string.Empty,
                    OptionsJson = ChoiceOptions.ToJson(options),
                    CorrectOption = correct,
                    SortOrder = q.SortOrder <= 0 ? order : q.SortOrder,
                    PromptImageMediaAssetId = q.PromptImageMediaAssetId
                });
            }
            else
            {
                existing.Prompt = q.Prompt.Trim();
                existing.OptionA = a ?? string.Empty;
                existing.OptionB = b ?? string.Empty;
                existing.OptionC = c ?? string.Empty;
                existing.OptionsJson = ChoiceOptions.ToJson(options);
                existing.CorrectOption = correct;
                existing.SortOrder = q.SortOrder <= 0 ? order : q.SortOrder;
                existing.PromptImageMediaAssetId = q.PromptImageMediaAssetId ?? existing.PromptImageMediaAssetId;
            }

            order++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (!wasPublished && quiz.IsPublished)
        {
            await notifications.NotifyQuizCreatedAsync(quiz, cancellationToken);
        }

        var updated = await dbContext.Quizzes
            .AsNoTracking()
            .Include(x => x.Questions)
            .FirstAsync(x => x.Id == quiz.Id, cancellationToken);
        return GetQuizzesQueryHandler.Map(updated);
    }
}
