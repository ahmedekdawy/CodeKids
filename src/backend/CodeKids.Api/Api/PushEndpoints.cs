using CodeKids.Domain.Entities;
using CodeKids.Infrastructure;
using CodeKids.Infrastructure.Notifications;
using CodeKids.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public sealed record PushConfigDto(string PublicKey);

public sealed record SavePushSubscriptionRequest(string? Endpoint, string? P256dh, string? Auth);

public sealed record RemovePushSubscriptionRequest(string? Endpoint);

public static class PushEndpoints
{
    public static IEndpointRouteBuilder MapPushEndpoints(this IEndpointRouteBuilder app)
    {
        // The key the browser needs to create a subscription that only this server can send to.
        app.MapGet("/api/push/config", async (
            AppDbContext dbContext,
            WebPushKeyStore keyStore,
            CancellationToken cancellationToken) =>
        {
            var keys = await keyStore.GetAsync(dbContext, cancellationToken);
            return Results.Ok(new PushConfigDto(keys.PublicKey));
        }).RequireAuthorization();

        // Registers this device for the signed-in user. A device has one subscription, so signing in as
        // someone else on the same device moves it to them.
        app.MapPost("/api/push/subscriptions", async (
            SavePushSubscriptionRequest request,
            HttpContext httpContext,
            AppDbContext dbContext,
            ITenantContext tenantContext,
            CancellationToken cancellationToken) =>
        {
            var endpoint = (request.Endpoint ?? string.Empty).Trim();
            var p256dh = (request.P256dh ?? string.Empty).Trim();
            var auth = (request.Auth ?? string.Empty).Trim();
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || endpoint.Length > PushSubscription.MaxEndpointLength
                || p256dh.Length is 0 or > PushSubscription.MaxKeyLength
                || auth.Length is 0 or > PushSubscription.MaxKeyLength)
            {
                return Results.BadRequest(new { message = "Push subscription is not valid." });
            }

            var userId = CurrentUser.GetUserId(httpContext.User);
            var subscription = await dbContext.PushSubscriptions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Endpoint == endpoint, cancellationToken);
            if (subscription is null)
            {
                subscription = new PushSubscription { Id = Guid.NewGuid(), Endpoint = endpoint };
                dbContext.PushSubscriptions.Add(subscription);
            }

            subscription.UserId = userId;
            subscription.TenantId = string.IsNullOrWhiteSpace(tenantContext.TenantId) ? null : tenantContext.TenantId;
            subscription.P256dh = p256dh;
            subscription.Auth = auth;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        // Called on sign-out and when notifications are switched off. Anonymous on purpose: by sign-out the
        // token is already gone, and knowing the endpoint URL is what proves ownership of the device.
        app.MapPost("/api/push/unsubscribe", async (
            RemovePushSubscriptionRequest request,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var endpoint = (request.Endpoint ?? string.Empty).Trim();
            if (endpoint.Length is 0 or > PushSubscription.MaxEndpointLength)
            {
                return Results.NoContent();
            }

            await dbContext.PushSubscriptions
                .IgnoreQueryFilters()
                .Where(x => x.Endpoint == endpoint)
                .ExecuteDeleteAsync(cancellationToken);
            return Results.NoContent();
        }).AllowAnonymous();

        return app;
    }
}
