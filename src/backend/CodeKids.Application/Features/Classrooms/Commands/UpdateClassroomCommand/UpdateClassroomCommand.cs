using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record UpdateClassroomRequest(
    string Name,
    string? Description,
    int? Grade,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses,
    string? WhatsAppGroupInviteUrl,
    string? ZoomMeetingLink,
    string? WhatsAppNotifyPhones);

public sealed record UpdateClassroomCommand(
    Guid ClassroomId,
    string Name,
    string? Description,
    int? Grade,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses,
    string? WhatsAppGroupInviteUrl,
    string? ZoomMeetingLink,
    string? WhatsAppNotifyPhones) : ICommand<ClassroomDto>;
