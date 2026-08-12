using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record TeacherLessonVideoDto(
    Guid Id,
    Guid LessonId,
    string LessonTitle,
    Guid CourseId,
    string CourseTitle,
    Guid MediaAssetId,
    string Title,
    string FileName,
    long SizeBytes,
    int? DurationSeconds,
    int SortOrder,
    DateTimeOffset CreatedAtUtc);
