using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Admin;

public sealed record SetCoursePublishedRequest(bool IsPublished);

public sealed record SetCoursePublishedCommand(
    Guid CourseId,
    bool IsPublished) : ICommand<CourseSummaryDto>;
