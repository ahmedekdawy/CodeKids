using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.LearningMaterials;

public sealed record DeleteLearningMaterialCommand(Guid TeacherUserId, string? Role, Guid MaterialId)
    : ICommand<bool>;
