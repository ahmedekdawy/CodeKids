using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.ZoomConnect;

public sealed record SaveZoomOAuthSettingsCommand(
    string ClientId,
    string? ClientSecret,
    string? RedirectUri,
    string? FrontendRedirectUri) : ICommand<ZoomUserOAuthSettingsDto>;
