using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260815140000_AddTenantSignups")]
    public partial class AddTenantSignups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "TenantSignups" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64) NULL,
                    "TenantName" character varying(120) NOT NULL,
                    "TenantSlug" character varying(64) NOT NULL,
                    "Email" character varying(160) NOT NULL,
                    "DisplayName" character varying(80) NOT NULL,
                    "PasswordHash" character varying(200) NOT NULL,
                    "TokenHash" character varying(128) NOT NULL,
                    "ExpiresAtUtc" timestamp with time zone NOT NULL,
                    "VerifiedAtUtc" timestamp with time zone NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_TenantSignups" PRIMARY KEY ("Id")
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantSignups_Email" ON "TenantSignups" ("Email");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantSignups_TenantSlug" ON "TenantSignups" ("TenantSlug");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantSignups_TokenHash" ON "TenantSignups" ("TokenHash");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "TenantSignups";""");
        }
    }
}
