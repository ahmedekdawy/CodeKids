using CodeKids.Application.Features.Analytics;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Dashboard;

public sealed record GetTeacherDashboardQuery(Guid TeacherId) : IQuery<TeacherDashboardDto>;
