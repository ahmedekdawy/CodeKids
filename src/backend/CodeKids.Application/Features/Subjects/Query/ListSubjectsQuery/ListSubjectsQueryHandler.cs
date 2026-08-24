using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Subjects;

public sealed class ListSubjectsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListSubjectsQuery, IReadOnlyList<SubjectDto>>
{
    public async Task<IReadOnlyList<SubjectDto>> Handle(ListSubjectsQuery query, CancellationToken cancellationToken)
    {
        var subjects = dbContext.Subjects.AsNoTracking().AsQueryable();
        if (query.StageId is int stageId)
        {
            subjects = subjects.Where(x => x.StageId == stageId);
        }

        return await subjects
            .OrderBy(x => x.StageId)
            .ThenBy(x => x.Title)
            .ThenBy(x => x.Id)
            .Select(x => new SubjectDto(x.Id, x.Title, x.StageId, x.Code, x.Category, x.NameEn))
            .ToListAsync(cancellationToken);
    }
}
