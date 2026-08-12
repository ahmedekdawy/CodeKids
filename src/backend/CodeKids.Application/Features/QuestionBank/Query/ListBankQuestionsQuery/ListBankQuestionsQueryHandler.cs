using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

public sealed class ListBankQuestionsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListBankQuestionsQuery, IReadOnlyList<BankQuestionDto>>
{
    public async Task<IReadOnlyList<BankQuestionDto>> Handle(ListBankQuestionsQuery query, CancellationToken cancellationToken)
    {
        var items = await dbContext.BankQuestions
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Lesson)
            .Include(x => x.Children)
            .Where(x => x.ParentQuestionId == null && x.CreatedByUserId == query.TeacherUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (query.CourseId is Guid courseId)
        {
            items = items.Where(x => x.CourseId == courseId).ToList();
        }

        if (query.LessonId is Guid lessonId)
        {
            items = items.Where(x => x.LessonId == lessonId).ToList();
        }

        return items.Select(CreateBankQuestionCommandHandler.Map).ToList();
    }
}
