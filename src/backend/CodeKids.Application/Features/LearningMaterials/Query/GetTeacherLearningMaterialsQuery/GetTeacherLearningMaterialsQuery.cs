using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.LearningMaterials;

public sealed record GetTeacherLearningMaterialsQuery(Guid UserId, string? Role)
    : IQuery<IReadOnlyList<TeacherLearningMaterialDto>>;
