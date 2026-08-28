using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record CreateBankQuestionRequest(
    Guid CourseId,
    Guid? LessonId,
    string QuestionType,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder,
    Guid? PromptImageMediaAssetId,
    IReadOnlyList<BankChildQuestionInput>? Children);

public sealed record CreateBankQuestionCommand(
    Guid TeacherUserId,
    Guid CourseId,
    Guid? LessonId,
    string QuestionType,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder,
    Guid? PromptImageMediaAssetId,
    IReadOnlyList<BankChildQuestionInput>? Children) : ICommand<BankQuestionDto>;
