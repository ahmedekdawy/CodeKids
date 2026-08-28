using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record BankChildQuestionInput(
    string Prompt,
    string QuestionType,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string CorrectAnswer,
    int Points,
    int SortOrder,
    Guid? PromptImageMediaAssetId);
