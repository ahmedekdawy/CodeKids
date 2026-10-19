using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class RegisterMediaFromUrlCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<RegisterMediaFromUrlCommand, MediaAssetDto>
{
    public async Task<MediaAssetDto> Handle(
        RegisterMediaFromUrlCommand command,
        CancellationToken cancellationToken)
    {
        var url = MediaUploadRules.NormalizeExternalUrl(command.Url);
        var uri = new Uri(url);
        // The chat panel is built from the same key the video URL carries (youtu.be/<key>, watch?v=<key>,
        // studio.youtube.com/video/<key>/livestreaming, ...), and the teacher can switch it off.
        var youtubeKey = YouTubeVideoKey.TryExtract(url);
        var hasChat = youtubeKey is not null && command.HasChat != false;
        var title = (command.Title ?? string.Empty).Trim();
        if (title.Length > 260)
        {
            title = title[..260];
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = string.IsNullOrWhiteSpace(uri.Host)
                ? "Video link"
                : uri.Host;
        }

        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            StorageKey = string.Empty,
            ExternalUrl = url,
            FileName = title,
            ContentType = "video/external",
            SizeBytes = 0,
            DurationSeconds = null,
            UploadedByUserId = command.TeacherUserId,
            HasChat = hasChat,
            YoutubeVideoKey = youtubeKey,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.MediaAssets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MediaAssetDto(
            asset.Id,
            asset.FileName,
            asset.ContentType,
            asset.SizeBytes,
            asset.DurationSeconds,
            asset.CreatedAtUtc,
            asset.ExternalUrl,
            asset.HasChat,
            asset.YoutubeVideoKey);
    }
}
