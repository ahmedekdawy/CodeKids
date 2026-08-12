using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class GetAssignmentByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAssignmentByIdQuery, AssignmentDto?>
{
    public async Task<AssignmentDto?> Handle(GetAssignmentByIdQuery query, CancellationToken cancellationToken)
    {
        var includeKey = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase)
            || string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        return await CreateAssignmentCommandHandler.LoadAssignment(dbContext, query.AssignmentId, includeKey, cancellationToken);
    }
}
