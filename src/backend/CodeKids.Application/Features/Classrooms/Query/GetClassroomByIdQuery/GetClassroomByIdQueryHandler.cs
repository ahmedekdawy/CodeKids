using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class GetClassroomByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetClassroomByIdQuery, ClassroomDto?>
{
    public Task<ClassroomDto?> Handle(GetClassroomByIdQuery query, CancellationToken cancellationToken) =>
        CreateClassroomCommandHandler.LoadDto(dbContext, query.ClassroomId, cancellationToken);
}
