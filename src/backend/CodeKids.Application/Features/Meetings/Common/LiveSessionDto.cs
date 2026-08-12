using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Meetings;

public sealed record LiveSessionDto(
    Guid Id,
    string Title,
    string Description,
    Guid HostUserId,
    string HostName,
    Guid? CourseId,
    string? CourseTitle,
    Guid? ClassroomId,
    string? ClassroomName,
    DateTimeOffset StartsAtUtc,
    int DurationMinutes,
    string JoinUrl,
    string? StartUrl,
    bool WhatsAppNotified,
    string? WhatsAppShareUrl,
    string? WhatsAppStatus);
