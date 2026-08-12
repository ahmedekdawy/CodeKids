using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Appointments;

public sealed record UpdateAppointmentRequest(
    Guid TeacherId,
    Guid CourseId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Notes);

public sealed record UpdateAppointmentCommand(
    Guid AppointmentId,
    Guid TeacherId,
    Guid CourseId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Notes) : ICommand<AppointmentDto>;
