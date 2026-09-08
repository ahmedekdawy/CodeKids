using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Classrooms;

public sealed record UpdateStudentClassroomEnrollmentCommand(
    Guid ClassroomId,
    Guid StudentId,
    IReadOnlyList<Guid>? CourseIds = null) : ICommand<ClassroomDto>;
