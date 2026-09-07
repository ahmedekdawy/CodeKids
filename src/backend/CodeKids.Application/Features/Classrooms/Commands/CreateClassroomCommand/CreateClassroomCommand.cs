using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record CreateClassroomRequest(
    string Name,
    string? Description,
    int? Grade,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses,
    string? WhatsAppGroupInviteUrl,
    IReadOnlyList<ClassroomZoomLinkDto>? ZoomLinks,
    string? WhatsAppNotifyPhones);

public sealed record CreateClassroomCommand(
    string Name,
    string? Description,
    int? Grade,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses,
    string? WhatsAppGroupInviteUrl,
    IReadOnlyList<ClassroomZoomLinkDto>? ZoomLinks,
    string? WhatsAppNotifyPhones) : ICommand<ClassroomDto>;
