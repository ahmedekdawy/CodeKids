using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Meetings;

public sealed record GetMeetingsQuery(Guid ViewerUserId, string ViewerRole) : IQuery<IReadOnlyList<LiveSessionDto>>;
