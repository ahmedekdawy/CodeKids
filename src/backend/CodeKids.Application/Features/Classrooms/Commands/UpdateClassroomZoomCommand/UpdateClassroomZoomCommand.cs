using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Classrooms;

public sealed record UpdateClassroomZoomRequest(string? ZoomMeetingLink);

public sealed record UpdateClassroomZoomCommand(
    Guid ClassroomId,
    Guid ActorUserId,
    string ActorRole,
    string? ZoomMeetingLink) : ICommand<ClassroomDto>;
