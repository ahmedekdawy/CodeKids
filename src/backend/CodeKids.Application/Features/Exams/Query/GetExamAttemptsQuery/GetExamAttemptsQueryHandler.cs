using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed class GetExamAttemptsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetExamAttemptsQuery, IReadOnlyList<ExamAttemptDto>>
{
    public async Task<IReadOnlyList<ExamAttemptDto>> Handle(
        GetExamAttemptsQuery query,
        CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .FirstOrDefaultAsync(x => x.Id == query.ExamId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found.");

        if (exam.Classroom?.Courses.Any(t => t.TeacherId == query.TeacherUserId) != true)
        {
            throw new InvalidOperationException("Only the classroom teacher can review exam attempts.");
        }

        var attempts = await dbContext.ExamAttempts
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Exam)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .Where(x => x.ExamId == query.ExamId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        return attempts.Select(SubmitExamCommandHandler.MapAttempt).ToList();
    }
}
