using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.ZoomConnect;

public sealed class GetZoomOAuthSettingsQueryHandler(
    IZoomOAuthSettingsStore settingsStore) : IQueryHandler<GetZoomOAuthSettingsQuery, ZoomUserOAuthSettingsDto>
{
    public Task<ZoomUserOAuthSettingsDto> Handle(GetZoomOAuthSettingsQuery query, CancellationToken cancellationToken)
    {
        var snap = settingsStore.Get();
        var maskedSecret = string.IsNullOrWhiteSpace(snap.ClientSecret)
            ? string.Empty
            : snap.ClientSecret.Length <= 4
                ? new string('•', snap.ClientSecret.Length)
                : $"{new string('•', Math.Max(4, snap.ClientSecret.Length - 4))}{snap.ClientSecret[^4..]}";

        return Task.FromResult(new ZoomUserOAuthSettingsDto(
            settingsStore.IsUserOAuthConfigured,
            snap.ClientId,
            maskedSecret,
            !string.IsNullOrWhiteSpace(snap.ClientSecret),
            snap.RedirectUri,
            snap.FrontendRedirectUri,
            string.IsNullOrWhiteSpace(snap.RedirectUri)
                ? "http://localhost:5078/api/zoom/callback"
                : snap.RedirectUri));
    }
}
