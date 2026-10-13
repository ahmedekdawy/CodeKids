using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261013120000_AddWebPush")]
    public class AddWebPush : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "PushSubscriptions" (
                    "Id" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "Endpoint" character varying(1000) NOT NULL,
                    "P256dh" character varying(200) NOT NULL,
                    "Auth" character varying(200) NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "TenantId" character varying(64) NULL,
                    CONSTRAINT "PK_PushSubscriptions" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_PushSubscriptions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_PushSubscriptions_Endpoint" ON "PushSubscriptions" ("Endpoint");
                CREATE INDEX IF NOT EXISTS "IX_PushSubscriptions_UserId" ON "PushSubscriptions" ("UserId");
                CREATE INDEX IF NOT EXISTS "IX_PushSubscriptions_TenantId" ON "PushSubscriptions" ("TenantId");

                CREATE TABLE IF NOT EXISTS "WebPushKeys" (
                    "Id" integer NOT NULL,
                    "PublicKey" character varying(200) NOT NULL,
                    "PrivateKey" character varying(200) NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_WebPushKeys" PRIMARY KEY ("Id")
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "PushSubscriptions";
                DROP TABLE IF EXISTS "WebPushKeys";
                """);
        }
    }
}
