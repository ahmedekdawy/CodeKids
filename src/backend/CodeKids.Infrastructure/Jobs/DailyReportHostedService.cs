using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Analytics;
using CodeKids.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeKids.Infrastructure.Jobs;

/// <summary>
/// Sends daily WhatsApp digests to students and linked parents around 20:00 local time.
/// </summary>
public sealed class DailyReportHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyReportHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.Now;
                if (now.Hour == 20 && now.Minute < 10)
                {
                    await RunDailyPassAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Daily report job failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task RunDailyPassAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<RunDailyWhatsAppReportsCommand, DailyWhatsAppReportsResultDto>>();

        var result = await handler.Handle(new RunDailyWhatsAppReportsCommand(Force: false), cancellationToken);
        logger.LogInformation(
            "Daily WhatsApp digest finished. StudentAttempts={StudentAttempts} ParentAttempts={ParentAttempts} Sent={Sent} Failed={Failed} Skipped={Skipped}",
            result.StudentMessagesAttempted,
            result.ParentMessagesAttempted,
            result.SentCount,
            result.FailedCount,
            result.SkippedCount);
    }
}
