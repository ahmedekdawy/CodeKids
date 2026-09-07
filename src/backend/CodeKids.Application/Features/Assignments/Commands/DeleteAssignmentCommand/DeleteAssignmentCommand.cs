using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Assignments;

public sealed record DeleteAssignmentCommand(Guid TeacherUserId, Guid AssignmentId) : ICommand<bool>;
