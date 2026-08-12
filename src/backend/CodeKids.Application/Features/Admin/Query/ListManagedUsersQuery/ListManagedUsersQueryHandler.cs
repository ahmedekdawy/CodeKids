using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class ListManagedUsersQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListManagedUsersQuery, IReadOnlyList<ManagedUserDto>>
{
    public async Task<IReadOnlyList<ManagedUserDto>> Handle(ListManagedUsersQuery query, CancellationToken cancellationToken)
    {
        var users = dbContext.Users
            .AsNoTracking()
            .Include(x => x.CourseRates)
            .ThenInclude(x => x.Course)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Role) &&
            Enum.TryParse<UserRole>(query.Role, true, out var role))
        {
            users = users.Where(x => x.Role == role);
        }

        return (await users
            .OrderBy(x => x.Role)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken))
            .Select(CreateManagedUserCommandHandler.ToDto)
            .ToList();
    }
}
