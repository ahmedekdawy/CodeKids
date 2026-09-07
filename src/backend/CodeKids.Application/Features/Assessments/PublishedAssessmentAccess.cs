using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Assessments;

internal static class PublishedAssessmentAccess
{
    internal static bool CanViewUnpublished(string? role) =>
        string.Equals(role, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
}
