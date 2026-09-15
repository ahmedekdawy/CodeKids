using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.LearningMaterials;

public sealed record GetLearningMaterialPageQuery(
    Guid UserId,
    string? Role,
    Guid? CourseId,
    Guid? UnitId,
    Guid? LessonId) : IQuery<LearningMaterialPageDto>;
