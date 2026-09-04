using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Courses;

internal static class PublishedCourseAccess
{
    internal static bool CanViewUnpublished(string? role) =>
        string.Equals(role, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
}
