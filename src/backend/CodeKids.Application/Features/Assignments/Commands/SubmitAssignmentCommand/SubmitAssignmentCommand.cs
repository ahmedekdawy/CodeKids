using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed record SubmitAssignmentRequest(Guid AssignmentId, IReadOnlyList<AssignmentAnswerInput> Answers);

public sealed record SubmitAssignmentCommand(
    Guid StudentId,
    Guid AssignmentId,
    IReadOnlyList<AssignmentAnswerInput> Answers) : ICommand<AssignmentSubmissionDto>;
