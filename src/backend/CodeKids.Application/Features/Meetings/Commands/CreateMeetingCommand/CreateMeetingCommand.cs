using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Meetings;

public sealed record CreateMeetingRequest(
    string Title,
    string? Description,
    DateTimeOffset StartsAtUtc,
    int DurationMinutes,
    Guid ClassroomId,
    Guid? CourseId,
    bool NotifyWhatsApp);

public sealed record CreateMeetingCommand(
    Guid HostUserId,
    string Title,
    string? Description,
    DateTimeOffset StartsAtUtc,
    int DurationMinutes,
    Guid ClassroomId,
    Guid? CourseId,
    bool NotifyWhatsApp) : ICommand<LiveSessionDto>;
