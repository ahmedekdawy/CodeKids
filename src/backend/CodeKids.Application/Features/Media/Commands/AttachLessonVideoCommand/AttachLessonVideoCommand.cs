using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Media;

public sealed record AttachLessonVideoRequest(Guid MediaAssetId, string? Title = null, int SortOrder = 1);

public sealed record AttachLessonVideoCommand(
    Guid TeacherUserId,
    Guid? LessonId,
    Guid MediaAssetId,
    string? Title,
    int SortOrder,
    Guid? CourseId = null) : ICommand<LessonVideoDto>;
