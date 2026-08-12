using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record TeacherSolutionVideoDto(
    Guid AssignmentId,
    string AssignmentTitle,
    Guid ClassroomId,
    string ClassroomName,
    Guid MediaAssetId,
    string FileName,
    long SizeBytes,
    int? DurationSeconds,
    DateTimeOffset CreatedAtUtc);
