using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using CodeKids.Domain.Entities;
using CodeKids.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;
using PushSubscription = CodeKids.Domain.Entities.PushSubscription;

namespace CodeKids.Infrastructure.Notifications;

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    /// <summary>Contact the push services can reach about this sender: a mailto: or https: URL.</summary>
    public string Subject { get; set; } = "mailto:admin@codekids.local";
    /// <summary>Optional fixed VAPID keys. When empty, a pair is generated and stored in the database.</summary>
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}

/// <summary>Supplies the VAPID keys: from configuration when set, otherwise one generated pair per database.</summary>
public sealed class WebPushKeyStore(IOptions<WebPushOptions> options)
{
    private readonly ConcurrentDictionary<string, VapidDetails> _byDatabase = new();

    public async Task<VapidDetails> GetAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        if (!string.IsNullOrWhiteSpace(configured.PublicKey) && !string.IsNullOrWhiteSpace(configured.PrivateKey))
        {
            return new VapidDetails(configured.Subject, configured.PublicKey.Trim(), configured.PrivateKey.Trim());
        }

        var database = dbContext.Database.GetConnectionString() ?? string.Empty;
        if (_byDatabase.TryGetValue(database, out var cached))
        {
            return cached;
        }

        var stored = await dbContext.WebPushKeys.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (stored is null)
        {
            var generated = VapidHelper.GenerateVapidKeys();
            // Two instances starting together may both generate; the row id is fixed, so only one insert wins.
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO "WebPushKeys" ("Id", "PublicKey", "PrivateKey", "CreatedAtUtc")
                 VALUES ({WebPushKeyPair.SingletonId}, {generated.PublicKey}, {generated.PrivateKey}, {DateTimeOffset.UtcNow})
                 ON CONFLICT ("Id") DO NOTHING
                 """,
                cancellationToken);
            stored = await dbContext.WebPushKeys.AsNoTracking().FirstAsync(cancellationToken);
        }

        var details = new VapidDetails(configured.Subject, stored.PublicKey, stored.PrivateKey);
        _byDatabase[database] = details;
        return details;
    }
}

/// <summary>Hands notifications to the background sender so the request that raised them is not slowed down.</summary>
public sealed class WebPushQueue(ILogger<WebPushQueue> logger)
{
    internal sealed record Item(string? TenantId, Guid UserId, Guid NotificationId, string Title, string Body, string? TargetUrl);

    private readonly Channel<Item> _channel = Channel.CreateBounded<Item>(
        new BoundedChannelOptions(5000) { FullMode = BoundedChannelFullMode.DropOldest });

    internal ChannelReader<Item> Reader => _channel.Reader;

    public void Enqueue(string? tenantId, Guid userId, Guid notificationId, string title, string body, string? targetUrl)
    {
        if (!_channel.Writer.TryWrite(new Item(tenantId, userId, notificationId, title, body, targetUrl)))
        {
            logger.LogWarning("Web push queue rejected a notification for user {UserId}.", userId);
        }
    }
}

public sealed class WebPushWorker(
    WebPushQueue queue,
    WebPushKeyStore keyStore,
    TenantCatalog catalog,
    ILogger<WebPushWorker> logger) : BackgroundService
{
    private readonly WebPushClient _client = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendAsync(item, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send web push for user {UserId}.", item.UserId);
            }
        }
    }

    private async Task SendAsync(WebPushQueue.Item item, CancellationToken cancellationToken)
    {
        var tenant = catalog.FindById(item.TenantId) ?? catalog.Default;
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(tenant.ConnectionString)
            .Options;
        await using var dbContext = new AppDbContext(dbOptions, new FixedTenantContext(item.TenantId ?? tenant.Id));

        var subscriptions = await dbContext.PushSubscriptions
            .IgnoreQueryFilters()
            .Where(x => x.UserId == item.UserId)
            .ToListAsync(cancellationToken);
        if (subscriptions.Count == 0)
        {
            return;
        }

        var keys = await keyStore.GetAsync(dbContext, cancellationToken);
        var payload = BuildPayload(item);
        var gone = new List<PushSubscription>();

        foreach (var subscription in subscriptions)
        {
            try
            {
                await _client.SendNotificationAsync(
                    new WebPush.PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth),
                    payload,
                    keys,
                    cancellationToken);
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                // The browser dropped this subscription (uninstalled, permission revoked, expired).
                gone.Add(subscription);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Web push to one device of user {UserId} failed.", item.UserId);
            }
        }

        if (gone.Count > 0)
        {
            dbContext.PushSubscriptions.RemoveRange(gone);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>The shape Angular's service worker turns into a system notification without custom code.</summary>
    private static string BuildPayload(WebPushQueue.Item item)
    {
        var url = string.IsNullOrWhiteSpace(item.TargetUrl) ? "/" : item.TargetUrl;
        return JsonSerializer.Serialize(new
        {
            notification = new
            {
                title = item.Title,
                body = item.Body,
                icon = "/icons/icon-192.png",
                badge = "/icons/icon-192.png",
                dir = "auto",
                tag = item.NotificationId.ToString("N"),
                data = new
                {
                    onActionClick = new Dictionary<string, object>
                    {
                        ["default"] = new { operation = "navigateLastFocusedOrOpen", url }
                    }
                }
            }
        });
    }
}
