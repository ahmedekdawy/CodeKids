using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Progress;

public sealed record CompleteStepRequest(Guid LessonId, Guid StepId, string SubmittedAnswer);

public sealed record CompleteStepCommand(
    Guid UserId,
    Guid LessonId,
    Guid StepId,
    string SubmittedAnswer) : ICommand<CompleteStepResponse>;
