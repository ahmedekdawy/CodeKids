using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed class StartExamCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<StartExamCommand, ExamAttemptDto>
{
    public async Task<ExamAttemptDto> Handle(StartExamCommand command, CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
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

        var existing = await dbContext.ExamAttempts
            .Include(x => x.Student)
            .Include(x => x.Exam)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(
                x => x.ExamId == exam.Id && x.StudentId == command.StudentId,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.Status != ExamAttemptStatus.InProgress)
            {
                throw new InvalidOperationException("Exam already submitted.");
            }

            return SubmitExamCommandHandler.MapAttempt(existing);
        }

        var student = await dbContext.Users.FirstAsync(x => x.Id == command.StudentId, cancellationToken);
        var attempt = new ExamAttempt
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            StudentId = student.Id,
            Status = ExamAttemptStatus.InProgress,
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.ExamAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await SubmitExamCommandHandler.LoadAttempt(dbContext, attempt.Id, cancellationToken))!;
    }
}
