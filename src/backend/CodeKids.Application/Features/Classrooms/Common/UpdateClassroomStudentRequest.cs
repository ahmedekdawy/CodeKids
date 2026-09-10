namespace CodeKids.Application.Features.Classrooms;

public sealed record UpdateClassroomStudentRequest(IReadOnlyList<Guid>? CourseIds = null);
