using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record UpdateClassroomAssignmentsCommand(
    Guid ClassroomId,
    IReadOnlyList<ClassroomCourseAssignmentRequest>? Courses) : ICommand<ClassroomDto>;
