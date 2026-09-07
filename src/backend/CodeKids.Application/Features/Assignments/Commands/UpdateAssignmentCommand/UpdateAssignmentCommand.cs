using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Assignments;

public sealed record UpdateAssignmentRequest(
    Guid ClassroomId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    bool IsPublished,
    IReadOnlyList<AssignmentQuestionInput> Questions);

public sealed record UpdateAssignmentCommand(
    Guid TeacherUserId,
    Guid AssignmentId,
    Guid ClassroomId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    bool IsPublished,
    IReadOnlyList<AssignmentQuestionInput> Questions) : ICommand<AssignmentDto>;
