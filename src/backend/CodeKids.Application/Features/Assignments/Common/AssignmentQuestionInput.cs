using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed record AssignmentQuestionInput(
    string Prompt,
    string QuestionType,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string CorrectAnswer,
    int Points,
    int SortOrder);
