using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Notifications;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed class PublishExamCommandHandler(IAppDbContext dbContext, NotificationPublisher notifications)
    : ICommandHandler<PublishExamCommand, ExamDto>
{
    public async Task<ExamDto> Handle(PublishExamCommand command, CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .Include(x => x.Questions)
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.ExamId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found.");

        var isAdmin = await dbContext.Users.AnyAsync(
            x => x.Id == command.TeacherUserId && x.Role == UserRole.SuperAdmin,
            cancellationToken);
        if (!isAdmin && exam.Classroom?.Courses.Any(t => t.TeacherId == command.TeacherUserId) != true)
        {
            throw new InvalidOperationException("Only an assigned classroom teacher can publish exams.");
        }

        var wasPublished = exam.IsPublished;
        exam.IsPublished = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!wasPublished)
        {
            await notifications.NotifyExamCreatedAsync(exam, cancellationToken);
        }

        return (await CreateExamCommandHandler.LoadExam(
            dbContext, exam.Id, includeAnswerKey: true, cancellationToken))!;
    }
}
