using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Grades;

public sealed class ListGradesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListGradesQuery, IReadOnlyList<GradeDto>>
{
    public async Task<IReadOnlyList<GradeDto>> Handle(ListGradesQuery query, CancellationToken cancellationToken)
    {
        var grades = dbContext.Grades.AsNoTracking().AsQueryable();
        if (query.StageId is int stageId)
        {
            grades = grades.Where(x => x.StageId == stageId);
        }

        return await grades
            .OrderBy(x => x.Id)
            .Select(x => new GradeDto(x.Id, x.Name, x.NameEn, x.StageId))
            .ToListAsync(cancellationToken);
    }
}
