using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SiteSettings;

public sealed record UploadSiteImageCommand(
    Guid AdminUserId,
    string Kind,
    string StorageKey,
    string ContentType) : ICommand<SiteSettingsDto>;
