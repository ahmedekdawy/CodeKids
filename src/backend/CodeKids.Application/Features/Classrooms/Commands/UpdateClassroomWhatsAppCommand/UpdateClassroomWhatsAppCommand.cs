using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record UpdateClassroomWhatsAppRequest(
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones,
    bool? DailyWhatsAppReportsEnabled);

public sealed record UpdateClassroomWhatsAppCommand(
    Guid ClassroomId,
    string? WhatsAppGroupInviteUrl,
    string? WhatsAppNotifyPhones,
    bool? DailyWhatsAppReportsEnabled) : ICommand<ClassroomDto>;
