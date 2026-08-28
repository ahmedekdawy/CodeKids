using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed record ExamQuestionDto(
    Guid Id,
    Guid? BankQuestionId,
    Guid? ParentExamQuestionId,
    string QuestionType,
    string Prompt,
    string PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<ChoiceOptionDto> Options,
    int Points,
    int SortOrder,
    string? CorrectAnswer,
    string? PromptImageUrl,
    IReadOnlyList<ExamQuestionDto> Children);
