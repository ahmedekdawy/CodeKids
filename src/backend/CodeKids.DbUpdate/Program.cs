using CodeKids.Infrastructure;
using CodeKids.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var apiDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CodeKids.Api"));
var configuration = new ConfigurationBuilder()
    .SetBasePath(apiDir)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var catalog = new TenantCatalog(configuration);
var hasher = new PasswordHasher();

Console.WriteLine($"Updating {catalog.All.Count} tenant database(s) using {apiDir}\\appsettings.json");

foreach (var tenant in catalog.All)
{
    Console.WriteLine($"Migrating tenant '{tenant.Id}'...");
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(tenant.ConnectionString)
        .Options;
    await using var db = new AppDbContext(options);
    await TenantSchema.EnsureAsync(db);
    await DataSeeder.SeedAsync(db, hasher);

    var classroomCount = await db.Classrooms.CountAsync();
    var userCount = await db.Users.CountAsync();
    var assignmentCount = await db.Assignments.CountAsync();
    var liveSessionCount = await db.LiveSessions.CountAsync();

    Console.WriteLine($"Tenant '{tenant.Id}' updated. Users: {userCount}; Classrooms: {classroomCount}; Assignments: {assignmentCount}; LiveSessions: {liveSessionCount}");
}

Console.WriteLine("Database update finished.");
