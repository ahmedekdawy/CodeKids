using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.ZoomConnect;

public sealed record GetZoomStatusQuery(Guid TeacherUserId) : IQuery<ZoomConnectionStatus>;
