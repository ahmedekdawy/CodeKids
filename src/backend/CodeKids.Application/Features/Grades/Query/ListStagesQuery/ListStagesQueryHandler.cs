using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Grades;

public sealed class ListStagesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListStagesQuery, IReadOnlyList<StageDto>>
{
    public async Task<IReadOnlyList<StageDto>> Handle(ListStagesQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Stages
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new StageDto(x.Id, x.Name, x.NameEn))
            .ToListAsync(cancellationToken);
    }
}
