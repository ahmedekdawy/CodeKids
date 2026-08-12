using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed record DeleteCourseUnitCommand(Guid UnitId) : ICommand<bool>;
