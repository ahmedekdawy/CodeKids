using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Assignments;

public sealed record PublishAssignmentCommand(Guid TeacherUserId, Guid AssignmentId) : ICommand<AssignmentDto>;
