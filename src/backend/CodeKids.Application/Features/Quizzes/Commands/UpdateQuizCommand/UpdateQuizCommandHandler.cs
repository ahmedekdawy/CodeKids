using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Assessments;
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
        quiz.DurationMinutes = AssessmentDuration.Normalize(command.DurationMinutes);
        var wasPublished = quiz.IsPublished;
        quiz.IsPublished = command.IsPublished;

        await QuizQuestionSync.ApplyAsync(dbContext, quiz, command.Questions, cancellationToken);

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
