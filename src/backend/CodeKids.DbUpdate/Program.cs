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
    ?? throw new InvalidOperationException("Connection string DefaultConnection is missing.");

Console.WriteLine($"Updating database using {apiDir}\\appsettings.json");

var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
services.AddSingleton<IPasswordHasher, PasswordHasher>();

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

await SchemaBootstrap.EnsureAsync(db);
await db.Database.MigrateAsync();
await DataSeeder.SeedAsync(db, hasher);

var classroomCount = await db.Classrooms.CountAsync();
var userCount = await db.Users.CountAsync();
var assignmentCount = await db.Assignments.CountAsync();
var liveSessionCount = await db.LiveSessions.CountAsync();

Console.WriteLine("Database updated successfully.");
Console.WriteLine($"Users: {userCount}");
Console.WriteLine($"Classrooms: {classroomCount}");
Console.WriteLine($"Assignments: {assignmentCount}");
Console.WriteLine($"LiveSessions: {liveSessionCount}");
