using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record ClassroomStudentDto(
    Guid StudentId,
    string DisplayName,
    string Email,
    string MobilePhone,
    IReadOnlyList<Guid> EnrolledCourseIds,
    IReadOnlyList<string> EnrolledCourseTitles);
