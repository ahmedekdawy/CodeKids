using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed record SubmitExamRequest(Guid ExamId, IReadOnlyList<ExamAnswerInput> Answers);

public sealed record SubmitExamCommand(
    Guid StudentId,
    Guid ExamId,
    IReadOnlyList<ExamAnswerInput> Answers) : ICommand<ExamAttemptDto>;
