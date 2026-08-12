using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed record CreateAssignmentRequest(
    Guid ClassroomId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    IReadOnlyList<AssignmentQuestionInput> Questions);

public sealed record CreateAssignmentCommand(
    Guid TeacherUserId,
    Guid ClassroomId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    IReadOnlyList<AssignmentQuestionInput> Questions) : ICommand<AssignmentDto>;
