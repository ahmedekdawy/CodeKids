using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record UpdateBankQuestionRequest(
    Guid? LessonId,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder);

public sealed record UpdateBankQuestionCommand(
    Guid TeacherUserId,
    Guid QuestionId,
    Guid? LessonId,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder) : ICommand<BankQuestionDto>;
