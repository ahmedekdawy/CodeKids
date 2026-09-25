namespace CodeKids.Application.Features.LearningMaterials;

public sealed record LearningMaterialDto(
    Guid Id,
    Guid CourseId,
    Guid? UnitId,
    Guid? LessonId,
    Guid MediaAssetId,
    string Title,
    string Kind,
    string FileName,
    string ContentType,
    long SizeBytes,
    int SortOrder,
    DateTimeOffset CreatedAtUtc);

public sealed record LearningMaterialPageDto(
    string Scope,
    Guid CourseId,
    string CourseTitle,
    Guid? UnitId,
    string? UnitTitle,
    Guid? LessonId,
    string? LessonTitle,
    IReadOnlyList<LearningMaterialDto> Items);

public sealed record TeacherLearningMaterialDto(
    Guid Id,
    string Scope,
    Guid CourseId,
    string CourseTitle,
    Guid? UnitId,
    string? UnitTitle,
    Guid? LessonId,
    string? LessonTitle,
    Guid MediaAssetId,
    string Title,
    string Kind,
    string FileName,
    string ContentType,
    long SizeBytes,
    int SortOrder,
    DateTimeOffset CreatedAtUtc);

internal static class LearningMaterialMapper
{
    public static string ScopeOf(Guid? unitId, Guid? lessonId) =>
        lessonId is not null ? "lesson" : unitId is not null ? "unit" : "course";

    public static LearningMaterialDto ToDto(Domain.Entities.LearningMaterial material)
    {
        var media = material.MediaAsset;
        return new LearningMaterialDto(
            material.Id,
            material.CourseId,
            material.UnitId,
            material.LessonId,
            material.MediaAssetId,
            material.Title,
            material.Kind.ToString(),
            media?.FileName ?? "",
            media?.ContentType ?? "",
            media?.SizeBytes ?? 0,
            material.SortOrder,
            material.CreatedAtUtc);
    }
}
