using System.Data;
using CodeKids.Infrastructure;
using CodeKids.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var apiDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "backend", "CodeKids.Api"));
var configuration = new ConfigurationBuilder()
    .SetBasePath(apiDir)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var catalog = new TenantCatalog(configuration);

for (var attempt = 1; attempt <= 4; attempt++)
{
    try
    {
        foreach (var tenant in catalog.All)
        {
            Console.WriteLine($"== tenant '{tenant.Id}' (attempt {attempt}) ==");
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(tenant.ConnectionString)
                .Options;
            await using var db = new AppDbContext(options, new FixedTenantContext(tenant.Id));
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """SELECT COALESCE("TenantId",'<null>'), count(*) FROM "Users" GROUP BY 1 ORDER BY 2 DESC""";
                await using var r = await cmd.ExecuteReaderAsync();
                Console.WriteLine("Users by TenantId:");
                while (await r.ReadAsync()) Console.WriteLine($"  {r.GetString(0)}: {r.GetInt64(1)}");
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT u."TenantId", u."Role", u."Email", u."DisplayName", u."MobilePhone"
                    FROM "Users" u
                    WHERE u."MobilePhone" IS NOT NULL
                      AND u."MobilePhone" IN (
                        SELECT "MobilePhone" FROM "Users"
                        WHERE "MobilePhone" IS NOT NULL
                        GROUP BY 1 HAVING count(*) > 1)
                    ORDER BY u."MobilePhone", u."Email"
                    LIMIT 40
                    """;
                await using var r = await cmd.ExecuteReaderAsync();
                Console.WriteLine("Users sharing a mobile phone:");
                var any = false;
                while (await r.ReadAsync())
                {
                    any = true;
                    Console.WriteLine($"  mobile={r.GetString(4)} | tenant={(r.IsDBNull(0) ? "<null>" : r.GetString(0))} | role={r.GetString(1)} | email={r.GetString(2)} | name={r.GetString(3)}");
                }
                if (!any) Console.WriteLine("  (none)");
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT EXISTS (
                      SELECT 1 FROM "__EFMigrationsHistory"
                      WHERE "MigrationId" = '20261004120000_AddGradeCertificateApproval')
                    """;
                var result = (bool)(await cmd.ExecuteScalarAsync()!);
                Console.WriteLine($"Approval migration applied: {result}");
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT count(*) FROM \"GradeCertificates\"";
                Console.WriteLine($"GradeCertificates rows: {await cmd.ExecuteScalarAsync()}");
            }
        }
        return;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
        if (attempt < 4) await Task.Delay(TimeSpan.FromSeconds(8));
    }
}
Environment.ExitCode = 1;
