using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Classrooms;

public sealed record UpdateClassroomZoomRequest(IReadOnlyList<ClassroomZoomLinkDto>? ZoomLinks);

public sealed record UpdateClassroomZoomCommand(
    Guid ClassroomId,
    Guid ActorUserId,
    string ActorRole,
    IReadOnlyList<ClassroomZoomLinkDto>? ZoomLinks) : ICommand<ClassroomDto>;
