using System.Threading.Channels;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Notifications;
using CodeKids.Application.Options;
using CodeKids.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Jobs;

public sealed class AssessmentPublishNotificationQueue(ILogger<AssessmentPublishNotificationQueue> logger)
    : IAssessmentPublishNotificationQueue
{
    internal sealed record Item(string? TenantId, AssessmentPublishInfo Info, IReadOnlyCollection<Guid> StudentIds);

    private readonly Channel<Item> channel = Channel.CreateBounded<Item>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.Wait });

    internal ChannelReader<Item> Reader => channel.Reader;

    public void Enqueue(string? tenantId, AssessmentPublishInfo info, IReadOnlyCollection<Guid> studentIds)
    {
        if (!channel.Writer.TryWrite(new Item(tenantId, info, studentIds)))
        {
            // Wait mode rarely fails; keep a fallback path so publish never loses the job silently.
            _ = channel.Writer.WriteAsync(new Item(tenantId, info, studentIds)).AsTask().ContinueWith(
                t => logger.LogWarning(
                    t.Exception,
                    "Failed to enqueue {Kind} publish WhatsApp notification for {Title}.",
                    info.Kind,
                    info.Title),
                TaskContinuationOptions.OnlyOnFaulted);
        }
        else
        {
            logger.LogInformation(
                "Queued {Kind} publish WhatsApp for {Title} ({StudentCount} students, tenant {TenantId}).",
                info.Kind,
                info.Title,
                studentIds.Count,
                tenantId);
        }
    }
}

/// <summary>
/// Drains <see cref="AssessmentPublishNotificationQueue"/> and sends the assessment link to every
/// enrolled student, parent, and teacher. Opens its own tenant DB connection for each job.
/// </summary>
public sealed class AssessmentPublishNotificationWorker(
    AssessmentPublishNotificationQueue queue,
    TenantCatalog catalog,
    IWhatsAppMessageSender whatsAppSender,
    IOptions<FrontendOptions> frontendOptions,
    IOptions<NotificationOptions> notificationOptions,
    ILoggerFactory loggerFactory,
    ILogger<AssessmentPublishNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Assessment publish WhatsApp worker started.");

        await foreach (var item in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await DispatchAsync(item, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to dispatch {Kind} publish WhatsApp notifications for {Title}.",
                    item.Info.Kind,
                    item.Info.Title);
            }
        }
    }

    private async Task DispatchAsync(
        AssessmentPublishNotificationQueue.Item item,
        CancellationToken cancellationToken)
    {
        var tenant = catalog.FindById(item.TenantId) ?? catalog.Default;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(tenant.ConnectionString)
            .Options;

        await using var dbContext = new AppDbContext(options, new FixedTenantContext(tenant.Id));

        var notifier = new AssessmentPublishWhatsAppNotifier(
            dbContext,
            whatsAppSender,
            Options.Create(new FrontendOptions { BaseUrl = ResolveFrontendBaseUrl(tenant) }),
            notificationOptions,
            loggerFactory.CreateLogger<AssessmentPublishWhatsAppNotifier>());

        await notifier.SendAsync(item.Info, item.StudentIds, cancellationToken);
    }

    private string ResolveFrontendBaseUrl(TenantInfo tenant)
    {
        if (!string.IsNullOrWhiteSpace(tenant.FrontendBaseUrl))
        {
            return tenant.FrontendBaseUrl.Trim().TrimEnd('/');
        }

        var host = tenant.Hosts.FirstOrDefault(h => h is not ("localhost" or "127.0.0.1"));
        return host is null
            ? (frontendOptions.Value.BaseUrl ?? string.Empty).Trim().TrimEnd('/')
            : $"https://{host}";
    }
}
