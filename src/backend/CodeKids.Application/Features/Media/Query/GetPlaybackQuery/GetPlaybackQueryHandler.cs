using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class GetPlaybackQueryHandler(
    IAppDbContext dbContext,
    IMediaAccessTokenService tokenService,
    Microsoft.Extensions.Options.IOptions<MediaOptions> mediaOptions,
    Microsoft.Extensions.Options.IOptions<TeraboxOptions> teraboxOptions)
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

        if (string.IsNullOrWhiteSpace(media.StorageKey))
        {
            throw new InvalidOperationException("Media asset has no storage key.");
        }

        var isTerabox = TeraboxStorageKey.IsTeraboxKey(media.StorageKey);
        var lifetime = TimeSpan.FromMinutes(Math.Clamp(mediaOptions.Value.SignedUrlMinutes, 1, 120));
        var token = tokenService.CreateToken(media.Id, user.Id, lifetime);
        var expires = DateTimeOffset.UtcNow.Add(lifetime);
        var baseUrl = query.BaseApiUrl.TrimEnd('/');
        var signedPlaybackUrl = $"{baseUrl}/media/stream?token={Uri.EscapeDataString(token)}";
        var contentType = MediaFileTypes.ResolveContentType(media.ContentType, media.FileName);

        return new PlaybackDto(
            media.Id,
            signedPlaybackUrl,
            watermark,
            expires,
            media.DurationSeconds,
            contentType,
            media.FileName,
            IsExternalLink: false,
            IsTeraboxHosted: isTerabox,
            TeraboxBaseUrl: isTerabox ? teraboxOptions.Value.BaseUrl.TrimEnd('/') : null);
    }
}
