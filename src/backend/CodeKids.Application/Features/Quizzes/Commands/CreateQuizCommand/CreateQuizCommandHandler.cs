using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Assessments;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Application.Features.Notifications;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class CreateQuizCommandHandler(IAppDbContext dbContext, NotificationPublisher notifications)
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

        var title = (command.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Quiz title is required.");
        }

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            CourseId = command.CourseId,
            ClassroomId = command.ClassroomId,
            CreatedByUserId = command.TeacherUserId,
            Title = title,
            Description = (command.Description ?? string.Empty).Trim(),
            XpReward = Math.Max(5, command.XpReward),
            DurationMinutes = AssessmentDuration.Normalize(command.DurationMinutes),
            IsPublished = command.IsPublished,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await QuizQuestionSync.ApplyAsync(dbContext, quiz, command.Questions, cancellationToken);

        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (quiz.IsPublished)
        {
            await notifications.NotifyQuizCreatedAsync(quiz, cancellationToken);
        }
        return GetQuizzesQueryHandler.Map(quiz);
    }
}
