using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed record CreateQuizRequest(
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    IReadOnlyList<CreateQuizQuestionInput> Questions);

public sealed record CreateQuizCommand(
    Guid TeacherUserId,
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    IReadOnlyList<CreateQuizQuestionInput> Questions) : ICommand<QuizDto>;
