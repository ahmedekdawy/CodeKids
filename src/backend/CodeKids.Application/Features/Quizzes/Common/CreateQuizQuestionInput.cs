using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed record CreateQuizQuestionInput(
    string Prompt,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    IReadOnlyList<string>? Options,
    string CorrectOption,
    int SortOrder);
