using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record AttachLessonVideoRequest(Guid MediaAssetId, string? Title = null, int SortOrder = 1);

public sealed record AttachLessonVideoCommand(
    Guid TeacherUserId,
    Guid LessonId,
    Guid MediaAssetId,
    string? Title,
    int SortOrder) : ICommand<LessonVideoDto>;
