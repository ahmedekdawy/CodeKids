using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Assessments;

internal static class TeacherAssessmentAccess
{
    internal static bool IsTeacher(string? role) =>
        string.Equals(role, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase);

    internal static bool IsAdmin(string? role) =>
        string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);

    internal static bool Owns(Guid? createdByUserId, Guid teacherUserId) =>
        createdByUserId == teacherUserId;

    /// <summary>Teachers see only what they created; admins see everything.</summary>
    internal static bool CanViewAsStaff(string? role, Guid? createdByUserId, Guid viewerUserId)
    {
        if (IsAdmin(role))
        {
            return true;
        }

        if (IsTeacher(role))
        {
            return Owns(createdByUserId, viewerUserId);
        }

        return true;
    }
}
