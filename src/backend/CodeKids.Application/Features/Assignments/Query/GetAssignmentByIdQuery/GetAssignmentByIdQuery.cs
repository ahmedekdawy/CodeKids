using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed record GetAssignmentByIdQuery(Guid AssignmentId, Guid ViewerUserId, string ViewerRole)
    : IQuery<AssignmentDto?>;
