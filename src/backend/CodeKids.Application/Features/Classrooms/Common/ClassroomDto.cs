using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record ClassroomDto(
    Guid Id,
    string Name,
    string Description,
    int? Grade,
    IReadOnlyList<ClassroomTeacherDto> Teachers,
    IReadOnlyList<ClassroomCourseDto> Courses,
    Guid? CourseId,
    string? CourseTitle,
    int? CourseGrade,
    int? CourseStageId,
    string? CourseSchoolType,
    string WhatsAppGroupInviteUrl,
    string ZoomMeetingLink,
    string WhatsAppNotifyPhones,
    bool DailyWhatsAppReportsEnabled,
    IReadOnlyList<ClassroomStudentDto> Students);
