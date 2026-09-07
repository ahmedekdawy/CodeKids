using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260830130000_AddUserNotifications")]
    public class AddUserNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "UserNotifications" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64),
                    "UserId" uuid NOT NULL,
                    "Kind" character varying(40) NOT NULL,
                    "Title" character varying(300) NOT NULL,
                    "Body" character varying(1000) NOT NULL,
                    "TargetUrl" character varying(500) NOT NULL,
                    "EntityId" uuid,
                    "RelatedStudentId" uuid,
                    "IsRead" boolean NOT NULL DEFAULT FALSE,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_UserNotifications" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_UserNotifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_UserNotifications_TenantId" ON "UserNotifications" ("TenantId");
                CREATE INDEX IF NOT EXISTS "IX_UserNotifications_UserId_IsRead_CreatedAtUtc" ON "UserNotifications" ("UserId", "IsRead", "CreatedAtUtc");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "UserNotifications";""");
        }
    }
}
