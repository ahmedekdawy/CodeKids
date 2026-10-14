using CodeKids.Application.Features.Tenants;
using CodeKids.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public static class TenantEndpoints
{
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

            string? siteName = null;
            if (validTenant)
            {
                siteName = await dbContext.SiteSettings
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId)
                    .Select(x => x.SiteName)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var name = string.IsNullOrWhiteSpace(siteName) ? "CodeKids" : siteName.Trim();
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
                icons = new object[]
                {
                    new { src = $"{appOrigin}/icons/icon-192.png", sizes = "192x192", type = "image/png", purpose = "any" },
                    new { src = $"{appOrigin}/icons/icon-512.png", sizes = "512x512", type = "image/png", purpose = "any" },
                    new { src = $"{appOrigin}/icons/icon-maskable-512.png", sizes = "512x512", type = "image/png", purpose = "maskable" }
                }
            };

            httpContext.Response.Headers.CacheControl = "public, max-age=300";
            return Results.Json(manifest, contentType: "application/manifest+json");
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
