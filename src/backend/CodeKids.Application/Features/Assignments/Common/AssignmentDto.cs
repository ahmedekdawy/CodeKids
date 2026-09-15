using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed record AssignmentDto(
    Guid Id,
    Guid ClassroomId,
    string ClassroomName,
    string Title,
    string Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    bool IsPublished,
    Guid CreatedByUserId,
    string CreatedByName,
    Guid? SolutionVideoMediaAssetId,
    Guid? CourseId,
    string? CourseTitle,
    IReadOnlyList<AssignmentQuestionDto> Questions,
    bool AlreadySubmitted = false);
