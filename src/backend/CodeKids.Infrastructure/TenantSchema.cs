using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace CodeKids.Infrastructure;

public static class TenantSchema
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await UsersTableExistsAsync(db, cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                DROP TABLE IF EXISTS "PasswordResetTokens" CASCADE;
                DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;
                """,
                cancellationToken);

            var creator = db.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync(cancellationToken);
            await StampAllMigrationsAsync(db, cancellationToken);
            return;
        }

        await db.Database.MigrateAsync(cancellationToken);
    }

    public static async Task StampTenantIdAsync(
        AppDbContext db,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var slug = (tenantId ?? string.Empty).Trim().ToLowerInvariant();
        if (slug.Length == 0)
        {
            return;
        }

        if (slug.Length > 64)
        {
            slug = slug[..64];
        }

        if (slug.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_'))
        {
            throw new InvalidOperationException($"Invalid tenant id '{tenantId}'.");
        }

#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(
            $"""
            DO $stamp$
            DECLARE r record;
            BEGIN
              FOR r IN
                SELECT c.table_name
                FROM information_schema.columns c
                JOIN information_schema.tables t
                  ON t.table_schema = c.table_schema
                 AND t.table_name = c.table_name
                WHERE c.table_schema = 'public'
                  AND c.column_name = 'TenantId'
                  AND t.table_type = 'BASE TABLE'
                  AND c.table_name <> 'TenantSignups'
              LOOP
                EXECUTE format(
                  'UPDATE %I SET "TenantId" = %L WHERE "TenantId" IS DISTINCT FROM %L',
                  r.table_name, '{slug}', '{slug}');
              END LOOP;
            END
            $stamp$;
            """,
            cancellationToken);
#pragma warning restore EF1002
    }

    private static async Task<bool> UsersTableExistsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'Users'
                )
                """;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is true;
        }
        finally
        {
            if (shouldClose)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task StampAllMigrationsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var history = db.GetService<IHistoryRepository>();
        var createScript = history.GetCreateIfNotExistsScript();
        if (!string.IsNullOrWhiteSpace(createScript))
        {
            await db.Database.ExecuteSqlRawAsync(createScript, cancellationToken);
        }

        var applied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        foreach (var id in db.Database.GetMigrations())
        {
            if (applied.Contains(id))
            {
                continue;
            }

            var insert = history.GetInsertScript(new HistoryRow(id, "10.0.4"));
            await db.Database.ExecuteSqlRawAsync(insert, cancellationToken);
        }
    }
}
