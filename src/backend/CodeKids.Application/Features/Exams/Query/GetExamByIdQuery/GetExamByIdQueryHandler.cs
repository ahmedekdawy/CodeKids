using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Assessments;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed class GetExamByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetExamByIdQuery, ExamDto?>
{
    public async Task<ExamDto?> Handle(GetExamByIdQuery query, CancellationToken cancellationToken)
    {
        var includeKey = PublishedAssessmentAccess.CanViewUnpublished(query.ViewerRole);

        var exam = await dbContext.Exams
            .AsNoTracking()
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Students)
            .Include(x => x.Course)
            .Include(x => x.CreatedBy)
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == query.ExamId, cancellationToken);

        if (exam is null)
        {
            return null;
        }

        var isStudent = string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);
        if (!includeKey && !exam.IsPublished)
        {
            return null;
        }

        if (isStudent && exam.Classroom?.Students.All(s => s.StudentId != query.ViewerUserId) != false)
        {
            return null;
        }

        return CreateExamCommandHandler.Map(exam, includeKey);
    }
}
