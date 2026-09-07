using CodeKids.Application.Features.Profile;

namespace CodeKids.Application.Features.WeeklyReports;

public static class TopStudentPhotoUrls
{
    /// <summary>
    /// Honour-board photos are shown on the anonymous login and landing pages, so they cannot use
    /// the authenticated user-photo route. This route serves the photo only while the student
    /// still qualifies for the board that week.
    /// </summary>
    public static string? Build(Guid studentId, DateOnly weekStart, string? storageKey) =>
        string.IsNullOrWhiteSpace(storageKey)
            ? null
            : $"/api/weekly-reports/top-students/{studentId}/photo" +
              $"?week={weekStart:yyyy-MM-dd}&v={ProfilePhotoUrls.Version(storageKey)}";
}
