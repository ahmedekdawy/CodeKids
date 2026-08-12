using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record AttachAssignmentSolutionVideoCommand(
    Guid TeacherUserId,
    Guid AssignmentId,
    Guid MediaAssetId) : ICommand<MediaAssetDto>;
