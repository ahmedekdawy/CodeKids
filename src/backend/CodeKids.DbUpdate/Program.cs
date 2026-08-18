using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var apiDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CodeKids.Api"));
var configuration = new ConfigurationBuilder()
    .SetBasePath(apiDir)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? configuration.GetConnectionString("EsraaConnection")
    ?? throw new InvalidOperationException("Connection string DefaultConnection or EsraaConnection is missing.");

Console.WriteLine($"Updating database using {apiDir}\\appsettings.json");

var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
services.AddSingleton<IPasswordHasher, PasswordHasher>();

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

await db.Database.MigrateAsync();
await DataSeeder.SeedAsync(db, hasher);

var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
var weeklyReportTableExists = await db.Database.SqlQueryRaw<int>(
        """
        SELECT COUNT(*)::int AS "Value"
        FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'StudentWeeklyReports'
        """)
    .SingleAsync();

var classroomCount = await db.Classrooms.CountAsync();
var userCount = await db.Users.CountAsync();
var assignmentCount = await db.Assignments.CountAsync();
var liveSessionCount = await db.LiveSessions.CountAsync();

Console.WriteLine("Database updated successfully.");
Console.WriteLine($"Applied migrations: {appliedMigrations.Count()} (latest: {appliedMigrations.LastOrDefault() ?? "none"})");
Console.WriteLine($"StudentWeeklyReports table: {(weeklyReportTableExists > 0 ? "exists" : "MISSING")}");
Console.WriteLine($"Users: {userCount}");
Console.WriteLine($"Classrooms: {classroomCount}");
Console.WriteLine($"Assignments: {assignmentCount}");
Console.WriteLine($"LiveSessions: {liveSessionCount}");
