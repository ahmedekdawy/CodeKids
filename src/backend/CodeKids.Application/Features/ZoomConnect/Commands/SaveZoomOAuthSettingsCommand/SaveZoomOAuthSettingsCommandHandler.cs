using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.ZoomConnect;

public sealed class SaveZoomOAuthSettingsCommandHandler(
    IZoomOAuthSettingsStore settingsStore) : ICommandHandler<SaveZoomOAuthSettingsCommand, ZoomUserOAuthSettingsDto>
{
    public Task<ZoomUserOAuthSettingsDto> Handle(SaveZoomOAuthSettingsCommand command, CancellationToken cancellationToken)
    {
        settingsStore.Save(command.ClientId, command.ClientSecret, command.RedirectUri, command.FrontendRedirectUri);
        return new GetZoomOAuthSettingsQueryHandler(settingsStore).Handle(new GetZoomOAuthSettingsQuery(), cancellationToken);
    }
}
