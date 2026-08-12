using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record SendClassroomWhatsAppRequest(
    string Message,
    IReadOnlyList<Guid>? StudentIds,
    bool IncludeGroupInviteLink = true);

public sealed record SendClassroomWhatsAppCommand(
    Guid TeacherUserId,
    Guid ClassroomId,
    string Message,
    IReadOnlyList<Guid>? StudentIds,
    bool IncludeGroupInviteLink) : ICommand<SendClassroomWhatsAppResultDto>;
