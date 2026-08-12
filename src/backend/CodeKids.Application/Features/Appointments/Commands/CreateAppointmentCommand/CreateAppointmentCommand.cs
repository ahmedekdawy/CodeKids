using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Appointments;

public sealed record CreateAppointmentRequest(
    Guid TeacherId,
    Guid CourseId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Notes,
    bool RepeatWeekly = false,
    DateTimeOffset? RepeatUntilUtc = null);

public sealed record CreateAppointmentCommand(
    Guid TeacherId,
    Guid CourseId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Notes,
    bool RepeatWeekly = false,
    DateTimeOffset? RepeatUntilUtc = null) : ICommand<CreateAppointmentsResult>;
