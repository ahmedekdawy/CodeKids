using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class GetPlaybackQueryHandler(
    IAppDbContext dbContext,
    IMediaAccessTokenService tokenService,
    Microsoft.Extensions.Options.IOptions<MediaOptions> mediaOptions)
    : IQueryHandler<GetPlaybackQuery, PlaybackDto>
{
    public async Task<PlaybackDto> Handle(GetPlaybackQuery query, CancellationToken cancellationToken)
    {
        var media = await dbContext.MediaAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.MediaAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Media asset not found.");

        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var watermark = $"{user.DisplayName} · {user.Email}";
        if (!string.IsNullOrWhiteSpace(media.ExternalUrl))
        {
            return new PlaybackDto(
                media.Id,
                media.ExternalUrl!,
                watermark,
                DateTimeOffset.UtcNow.AddHours(12),
                media.DurationSeconds,
                media.ContentType,
                media.FileName,
                IsExternalLink: true);
        }

        var lifetime = TimeSpan.FromMinutes(Math.Clamp(mediaOptions.Value.SignedUrlMinutes, 1, 120));
        var token = tokenService.CreateToken(media.Id, user.Id, lifetime);
        var expires = DateTimeOffset.UtcNow.Add(lifetime);
        var baseUrl = query.BaseApiUrl.TrimEnd('/');
        var playbackUrl = $"{baseUrl}/media/stream?token={Uri.EscapeDataString(token)}";

        return new PlaybackDto(
            media.Id,
            playbackUrl,
            watermark,
            expires,
            media.DurationSeconds,
            media.ContentType,
            media.FileName,
            IsExternalLink: false);
    }
}
