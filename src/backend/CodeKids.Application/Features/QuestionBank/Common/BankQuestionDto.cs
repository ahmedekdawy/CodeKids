using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record BankQuestionDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    Guid? LessonId,
    string? LessonTitle,
    Guid CreatedByUserId,
    Guid? ParentQuestionId,
    string QuestionType,
    string Prompt,
    string PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<ChoiceOptionDto> Options,
    string CorrectAnswer,
    int Points,
    int SortOrder,
    IReadOnlyList<BankQuestionDto> Children);
