using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.LearningMaterials;

public sealed record AttachLearningMaterialCommand(
    Guid TeacherUserId,
    string? Role,
    Guid? CourseId,
    Guid? UnitId,
    Guid? LessonId,
    Guid MediaAssetId,
    string? Title,
    int SortOrder) : ICommand<LearningMaterialDto>;
