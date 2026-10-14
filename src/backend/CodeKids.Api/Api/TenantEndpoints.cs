using CodeKids.Application.Features.Tenants;
using CodeKids.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public static class TenantEndpoints
{
    /// <summary>
    /// Relative links, so they resolve against wherever the manifest itself was fetched from. The version
    /// follows the logo, so replacing it makes installed apps fetch the new icon.
    /// </summary>
    private static object[] TenantIcons(string tenantId, string logoStorageKey)
    {
        var version = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(logoStorageKey)))[..12].ToLowerInvariant();
        string Src(int size, bool maskable = false) =>
            $"icon.png?tenant={Uri.EscapeDataString(tenantId)}&size={size}{(maskable ? "&maskable=true" : "")}&v={version}";

        return
        [
            new { src = Src(192), sizes = "192x192", type = "image/png", purpose = "any" },
            new { src = Src(512), sizes = "512x512", type = "image/png", purpose = "any" },
            new { src = Src(512, maskable: true), sizes = "512x512", type = "image/png", purpose = "maskable" }
        ];
    }

    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/tenants", async (
            IQueryHandler<ListTenantsQuery, IReadOnlyList<AdminTenantDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(new ListTenantsQuery(), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapGet("/api/tenants/default", (CodeKids.Infrastructure.Tenancy.TenantCatalog catalog) =>
            Results.Ok(new { tenantId = catalog.DefaultTenantId })).AllowAnonymous();

        // The installed app (PWA) takes its name from this manifest, so each tenant installs under its own name.
        // Anonymous and cross-origin: the browser and the app stores' install services fetch it without a login.
        app.MapGet("/api/tenants/manifest.webmanifest", async (
            string? tenant,
            string? app,
            HttpContext httpContext,
            CodeKids.Application.Abstractions.IAppDbContext dbContext,
            CodeKids.Infrastructure.Tenancy.TenantCatalog catalog,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            // Only our own frontends may be named as the app to open; this value ends up in start_url.
            var appOrigin = (app ?? string.Empty).Trim().TrimEnd('/');
            if (!CorsOrigins.IsAllowed(CorsOrigins.Resolve(configuration, catalog), appOrigin))
            {
                return Results.BadRequest(new { message = "Unknown app origin." });
            }

            var tenantId = (tenant ?? string.Empty).Trim().ToLowerInvariant();
            var validTenant = tenantId.Length is > 0 and <= 64
                && tenantId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

            var settings = validTenant
                ? await dbContext.SiteSettings
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId)
                    .Select(x => new { x.SiteName, x.LogoStorageKey })
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            var name = string.IsNullOrWhiteSpace(settings?.SiteName) ? "CodeKids" : settings.SiteName.Trim();
            var startUrl = validTenant ? $"{appOrigin}/?tenant={Uri.EscapeDataString(tenantId)}" : $"{appOrigin}/";
            var manifest = new
            {
                name,
                short_name = name,
                id = startUrl,
                start_url = startUrl,
                scope = $"{appOrigin}/",
                display = "standalone",
                orientation = "any",
                background_color = "#07111f",
                theme_color = "#0c1a2e",
                icons = string.IsNullOrWhiteSpace(settings?.LogoStorageKey)
                    ? new object[]
                    {
                        new { src = $"{appOrigin}/icons/icon-192.png", sizes = "192x192", type = "image/png", purpose = "any" },
                        new { src = $"{appOrigin}/icons/icon-512.png", sizes = "512x512", type = "image/png", purpose = "any" },
                        new { src = $"{appOrigin}/icons/icon-maskable-512.png", sizes = "512x512", type = "image/png", purpose = "maskable" }
                    }
                    : TenantIcons(tenantId, settings.LogoStorageKey)
            };

            httpContext.Response.Headers.CacheControl = "public, max-age=300";
            return Results.Json(manifest, contentType: "application/manifest+json");
        }).AllowAnonymous();

        // The tenant's logo as a square app icon. The manifest links here only when the tenant has a logo.
        app.MapGet("/api/tenants/icon.png", async (
            string? tenant,
            int? size,
            bool? maskable,
            HttpContext httpContext,
            CodeKids.Application.Abstractions.IAppDbContext dbContext,
            CodeKids.Application.Abstractions.IFileStorage fileStorage,
            CancellationToken cancellationToken) =>
        {
            var tenantId = (tenant ?? string.Empty).Trim().ToLowerInvariant();
            var pixels = size ?? 192;
            if (tenantId.Length is 0 or > 64 || !TenantIconRenderer.Sizes.Contains(pixels))
            {
                return Results.NotFound();
            }

            var logoKey = await dbContext.SiteSettings
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.LogoStorageKey)
                .FirstOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(logoKey))
            {
                return Results.NotFound();
            }

            var png = await TenantIconRenderer.RenderAsync(
                $"{tenantId}|{logoKey}",
                async () =>
                {
                    await using var stream = await fileStorage.OpenReadAsync(logoKey, cancellationToken);
                    using var buffer = new MemoryStream();
                    await stream.CopyToAsync(buffer, cancellationToken);
                    return buffer.ToArray();
                },
                pixels,
                maskable == true);
            if (png is null)
            {
                return Results.NotFound();
            }

            // The manifest's links carry the logo version, so a new logo gets new URLs and this can be cached long.
            httpContext.Response.Headers.CacheControl = "public, max-age=604800";
            return Results.File(png, "image/png");
        }).AllowAnonymous();

        app.MapPost("/api/tenants/register", async (
            RegisterTenantRequest request,
            ICommandHandler<RegisterTenantCommand, RegisterTenantResult> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.Handle(
                    new RegisterTenantCommand(
                        request.TenantName,
                        request.Email,
                        request.DisplayName,
                        request.Password,
                        request.MobilePhone,
                        request.TenantStyle,
                        request.MultiTimeZone == true,
                        request.TimeZone),
                    cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        });

        app.MapPost("/api/tenants/verify", async (
            VerifyTenantRequest request,
            ICommandHandler<VerifyTenantCommand, VerifyTenantResult> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.Handle(new VerifyTenantCommand(request.Token), cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        });

        return app;
    }
}
