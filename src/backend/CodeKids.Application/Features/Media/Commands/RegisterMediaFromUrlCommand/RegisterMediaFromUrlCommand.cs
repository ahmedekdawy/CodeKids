using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record RegisterMediaFromUrlRequest(string Url, string? Title = null);

public sealed record RegisterMediaFromUrlCommand(
    Guid TeacherUserId,
    string Url,
    string? Title = null) : ICommand<MediaAssetDto>;
