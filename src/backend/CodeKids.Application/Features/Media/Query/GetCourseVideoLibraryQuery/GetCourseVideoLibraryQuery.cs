using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Media;

public sealed record GetCourseVideoLibraryQuery(Guid AdminUserId)
    : IQuery<IReadOnlyList<CourseVideoLibraryItemDto>>;
