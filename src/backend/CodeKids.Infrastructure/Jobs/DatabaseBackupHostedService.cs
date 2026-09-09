using System.Diagnostics;
using System.Globalization;
using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Options;
using CodeKids.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CodeKids.Infrastructure.Jobs;

/// <summary>
/// Once per calendar day, dumps every tenant database with pg_dump and uploads the file to media storage.
/// </summary>
public sealed class DatabaseBackupHostedService(
    IServiceScopeFactory scopeFactory,
    TenantCatalog tenantCatalog,
    IOptions<BackupOptions> backupOptions,
    ILogger<DatabaseBackupHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AfterRunCooldown = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = backupOptions.Value;
                if (options.Enabled)
                {
                    var now = DateTimeOffset.Now;
                    var hour = Math.Clamp(options.HourLocal, 0, 23);
                    var window = Math.Clamp(options.MinuteWindow, 1, 59);
                    if (now.Hour == hour && now.Minute < window && !AlreadyRanToday(options, now))
                    {
                        await RunDailyPassAsync(options, now, stoppingToken);
                        await Task.Delay(AfterRunCooldown, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database backup job failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task RunDailyPassAsync(BackupOptions options, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(options.LocalRootPath);
        Directory.CreateDirectory(root);

        var stamp = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        logger.LogInformation("Starting daily database backup for {Date} ({TenantCount} tenants).", stamp, tenantCatalog.All.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var tenant in tenantCatalog.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await BackupTenantAsync(tenant, options, root, stamp, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                logger.LogError(ex, "Database backup failed for tenant {TenantId}.", tenant.Id);
            }
        }

        WriteStamp(root, stamp);
        CleanupOldLocalBackups(root, options.RetentionDays);

        logger.LogInformation(
            "Daily database backup finished. Success={Success} Failed={Failed} Date={Date}",
            successCount,
            failCount,
            stamp);
    }

    private async Task BackupTenantAsync(
        TenantInfo tenant,
        BackupOptions options,
        string root,
        string stamp,
        CancellationToken cancellationToken)
    {
        var fileName = $"db-backup-{Sanitize(tenant.Id)}-{stamp}.dump";
        var localPath = Path.Combine(root, fileName);

        logger.LogInformation("Dumping database for tenant {TenantId} to {Path}.", tenant.Id, localPath);
        await RunPgDumpAsync(tenant.ConnectionString, options.PgDumpPath, localPath, cancellationToken);

        var fileInfo = new FileInfo(localPath);
        if (!fileInfo.Exists || fileInfo.Length == 0)
        {
            throw new InvalidOperationException($"pg_dump produced an empty file for tenant {tenant.Id}.");
        }

        logger.LogInformation(
            "Database dump ready for tenant {TenantId}. SizeBytes={Size}",
            tenant.Id,
            fileInfo.Length);

        if (!options.UploadToMediaStorage)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        await using (var stream = File.OpenRead(localPath))
        {
            var storageKey = await storage.SaveAsync(
                stream,
                fileName,
                "application/octet-stream",
                cancellationToken);
            logger.LogInformation(
                "Uploaded database backup for tenant {TenantId}. StorageKey={StorageKey}",
                tenant.Id,
                storageKey);
        }

        if (!options.KeepLocalAfterUpload)
        {
            File.Delete(localPath);
        }
    }

    private static async Task RunPgDumpAsync(
        string connectionString,
        string pgDumpPath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Host) || string.IsNullOrWhiteSpace(builder.Database))
        {
            throw new InvalidOperationException("Connection string is missing Host or Database.");
        }

        var args = new StringBuilder();
        args.Append("-h ").Append(Quote(builder.Host));
        args.Append(" -p ").Append(builder.Port > 0 ? builder.Port : 5432);
        args.Append(" -U ").Append(Quote(builder.Username ?? string.Empty));
        args.Append(" -d ").Append(Quote(builder.Database));
        args.Append(" --format=custom --no-owner --no-acl");
        args.Append(" -f ").Append(Quote(outputPath));

        var start = new ProcessStartInfo
        {
            FileName = string.IsNullOrWhiteSpace(pgDumpPath) ? "pg_dump" : pgDumpPath,
            Arguments = args.ToString(),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(builder.Password))
        {
            start.Environment["PGPASSWORD"] = builder.Password;
        }

        // Aiven and similar hosts require TLS; pg_dump honors PGSSLMODE.
        start.Environment["PGSSLMODE"] = MapSslMode(builder.SslMode);

        using var process = new Process { StartInfo = start };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start pg_dump at '{start.FileName}'.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException(
                $"pg_dump exited with code {process.ExitCode}: {Truncate(detail)}");
        }
    }

    private static string MapSslMode(SslMode mode) => mode switch
    {
        SslMode.Disable => "disable",
        SslMode.Allow => "allow",
        SslMode.Prefer => "prefer",
        SslMode.Require => "require",
        SslMode.VerifyCA => "verify-ca",
        SslMode.VerifyFull => "verify-full",
        _ => "prefer"
    };

    private static bool AlreadyRanToday(BackupOptions options, DateTimeOffset now)
    {
        var stampPath = Path.Combine(Path.GetFullPath(options.LocalRootPath), ".last-backup-date");
        if (!File.Exists(stampPath))
        {
            return false;
        }

        try
        {
            var text = File.ReadAllText(stampPath).Trim();
            return string.Equals(text, now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static void WriteStamp(string root, string stamp)
    {
        File.WriteAllText(Path.Combine(root, ".last-backup-date"), stamp);
    }

    private static void CleanupOldLocalBackups(string root, int retentionDays)
    {
        if (retentionDays <= 0 || !Directory.Exists(root))
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        foreach (var file in Directory.EnumerateFiles(root, "db-backup-*.dump"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private static string Sanitize(string value)
    {
        var chars = value.Trim().Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-').ToArray();
        return new string(chars);
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    private static string Truncate(string value, int max = 800)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max] + "…";
    }
}
