using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed record ExamDto(
    Guid Id,
    Guid ClassroomId,
    string ClassroomName,
    Guid? CourseId,
    string? CourseTitle,
    string Title,
    string Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    bool IsPublished,
    Guid CreatedByUserId,
    string CreatedByName,
    IReadOnlyList<ExamQuestionDto> Questions);
