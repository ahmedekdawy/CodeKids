using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using CodeKids.Application.Features.Chat;
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
    /// <summary>Same tables as the AddWebPush migration; every statement is safe to run again.</summary>
    private const string CreateTablesSql =
        """
        CREATE TABLE IF NOT EXISTS "PushSubscriptions" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "Endpoint" character varying(1000) NOT NULL,
            "P256dh" character varying(200) NOT NULL,
            "Auth" character varying(200) NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "TenantId" character varying(64) NULL,
            CONSTRAINT "PK_PushSubscriptions" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_PushSubscriptions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_PushSubscriptions_Endpoint" ON "PushSubscriptions" ("Endpoint");
        CREATE INDEX IF NOT EXISTS "IX_PushSubscriptions_UserId" ON "PushSubscriptions" ("UserId");
        CREATE INDEX IF NOT EXISTS "IX_PushSubscriptions_TenantId" ON "PushSubscriptions" ("TenantId");

        CREATE TABLE IF NOT EXISTS "WebPushKeys" (
            "Id" integer NOT NULL,
            "PublicKey" character varying(200) NOT NULL,
            "PrivateKey" character varying(200) NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_WebPushKeys" PRIMARY KEY ("Id")
        );
        """;

    private readonly ConcurrentDictionary<string, VapidDetails> _byDatabase = new();
    private readonly ConcurrentDictionary<string, bool> _tablesReady = new();

    /// <summary>
    /// Migrations are applied by hand (CodeKids.DbUpdate), so a database can be running this code before
    /// the push tables exist. Creating them here keeps push working without waiting for that run.
    /// </summary>
    public async Task EnsureTablesAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var database = dbContext.Database.GetConnectionString() ?? string.Empty;
        if (_tablesReady.ContainsKey(database))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(CreateTablesSql, cancellationToken);
        _tablesReady[database] = true;
    }

    public async Task<VapidDetails> GetAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await EnsureTablesAsync(dbContext, cancellationToken);
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
    private const int PushTimeToLiveSeconds = 24 * 60 * 60;

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
        var payload = BuildPayload(item, await UnreadCountAsync(dbContext, item.UserId, cancellationToken));
        var options = new Dictionary<string, object>
        {
            ["vapidDetails"] = keys,
            // A phone that is off or offline gets the alert when it reconnects, but not once it is stale.
            ["TTL"] = PushTimeToLiveSeconds,
            // "high" makes the push service wake a sleeping phone (Android Doze) instead of holding the
            // push until the user next unlocks it, so alerts arrive while the app is closed.
            ["headers"] = new Dictionary<string, object> { ["Urgency"] = "high" }
        };
        var gone = new List<PushSubscription>();

        foreach (var subscription in subscriptions)
        {
            try
            {
                await _client.SendNotificationAsync(
                    new WebPush.PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth),
                    payload,
                    options,
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

    /// <summary>
    /// What the installed app's icon badge shows: unread notifications plus unread chat messages, the
    /// same two counts the open app adds up. Counted when sending, so the badge is right while closed.
    /// </summary>
    private static async Task<int> UnreadCountAsync(AppDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var notifications = await dbContext.UserNotifications
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);
        var roomIds = await dbContext.ChatRoomMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.RoomId)
            .ToListAsync(cancellationToken);
        var chat = (await ChatAccess.UnreadCountsAsync(dbContext, userId, roomIds, cancellationToken)).Values.Sum();
        return notifications + chat;
    }

    /// <summary>
    /// The shape Angular's service worker turns into a system notification without custom code.
    /// appBadge sits outside it and is read by app-sw.js, which wraps Angular's worker.
    /// </summary>
    private static string BuildPayload(WebPushQueue.Item item, int unread)
    {
        var url = string.IsNullOrWhiteSpace(item.TargetUrl) ? "/" : item.TargetUrl;
        return JsonSerializer.Serialize(new
        {
            appBadge = unread,
            notification = new
            {
                title = item.Title,
                body = item.Body,
                icon = "/icons/icon-192.png",
                badge = "/icons/icon-192.png",
                dir = "auto",
                vibrate = new[] { 200, 100, 200 },
                // Chat pushes reuse the room as their tag; renotify makes each new message alert again.
                renotify = true,
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
