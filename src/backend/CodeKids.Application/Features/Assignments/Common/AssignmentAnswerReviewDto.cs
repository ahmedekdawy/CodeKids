using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed record AssignmentAnswerReviewDto(
    Guid QuestionId,
    string Prompt,
    string AnswerText,
    string? CorrectAnswer,
    bool? IsCorrect,
    int? PointsAwarded,
    int Points,
    string? PromptImageUrl,
    string? AnswerImageUrl);
