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
