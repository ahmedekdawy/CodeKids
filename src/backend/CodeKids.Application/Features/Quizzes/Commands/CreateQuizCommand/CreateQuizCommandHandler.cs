using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class CreateQuizCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateQuizCommand, QuizDto>
{
    public async Task<QuizDto> Handle(CreateQuizCommand command, CancellationToken cancellationToken)
    {
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
                throw new InvalidOperationException("Only an assigned classroom teacher can create quizzes for that classroom.");
            }
        }
        else
        {
            var teaches = await dbContext.ClassroomCourses.AnyAsync(
                x => x.TeacherId == command.TeacherUserId,
                cancellationToken);
            if (!teaches)
            {
                throw new InvalidOperationException("Teacher must be assigned to a classroom before creating quizzes.");
            }
        }

        if (command.Questions is null || command.Questions.Count == 0)
        {
            throw new InvalidOperationException("Add at least one question.");
        }

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            CourseId = command.CourseId,
            ClassroomId = command.ClassroomId,
            CreatedByUserId = command.TeacherUserId,
            Title = command.Title.Trim(),
            Description = (command.Description ?? string.Empty).Trim(),
            XpReward = Math.Max(5, command.XpReward),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var order = 1;
        foreach (var q in command.Questions)
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
                SortOrder = q.SortOrder <= 0 ? order : q.SortOrder
            });
            order++;
        }

        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);
        return GetQuizzesQueryHandler.Map(quiz);
    }
}
