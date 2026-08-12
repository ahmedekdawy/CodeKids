using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record RecordWatchEventsRequest(
    Guid MediaAssetId,
    Guid? LessonId,
    Guid? SessionId,
    IReadOnlyList<WatchEventInput> Events);

public sealed record RecordWatchEventsCommand(
    Guid StudentId,
    Guid MediaAssetId,
    Guid? LessonId,
    Guid? SessionId,
    IReadOnlyList<WatchEventInput> Events) : ICommand<WatchSessionDto>;
