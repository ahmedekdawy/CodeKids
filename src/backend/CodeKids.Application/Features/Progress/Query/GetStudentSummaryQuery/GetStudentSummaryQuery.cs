using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Progress;

public sealed record GetStudentSummaryQuery(Guid UserId) : IQuery<StudentSummaryDto>;
