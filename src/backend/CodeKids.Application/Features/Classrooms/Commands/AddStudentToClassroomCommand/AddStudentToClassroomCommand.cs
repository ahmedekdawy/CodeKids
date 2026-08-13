using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed record AddStudentToClassroomCommand(
    Guid ClassroomId,
    Guid StudentId,
    IReadOnlyList<Guid>? CourseIds = null) : ICommand<EnrollStudentResultDto>;
