using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed record ExamAnswerReviewDto(
    Guid QuestionId,
    string Prompt,
    string QuestionType,
    string AnswerText,
    string? CorrectAnswer,
    bool? IsCorrect,
    int? PointsAwarded,
    int Points,
    string? PromptImageUrl);
