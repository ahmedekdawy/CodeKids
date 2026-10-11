using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261011130000_AddTimeZonesAndGender")]
    public class AddTimeZonesAndGender : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "MultiTimeZone" boolean NOT NULL DEFAULT false;
                ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "TimeZone" character varying(64) NULL;
                ALTER TABLE "TenantSignups" ADD COLUMN IF NOT EXISTS "MultiTimeZone" boolean NOT NULL DEFAULT false;
                ALTER TABLE "TenantSignups" ADD COLUMN IF NOT EXISTS "TimeZone" character varying(64) NULL;
                ALTER TABLE "StudentProfiles" ADD COLUMN IF NOT EXISTS "Gender" character varying(10) NULL;
                ALTER TABLE "StudentProfiles" ADD COLUMN IF NOT EXISTS "TimeZone" character varying(64) NULL;
                ALTER TABLE "StudentProfiles" ADD COLUMN IF NOT EXISTS "SessionMinutes" integer NULL;
                ALTER TABLE "StudentProfiles" ADD COLUMN IF NOT EXISTS "SessionsPerWeek" integer NULL;
                ALTER TABLE "TeacherProfiles" ADD COLUMN IF NOT EXISTS "Gender" character varying(10) NULL;
                ALTER TABLE "TeacherProfiles" ADD COLUMN IF NOT EXISTS "TimeZone" character varying(64) NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "SiteSettings" DROP COLUMN IF EXISTS "MultiTimeZone";
                ALTER TABLE "SiteSettings" DROP COLUMN IF EXISTS "TimeZone";
                ALTER TABLE "TenantSignups" DROP COLUMN IF EXISTS "MultiTimeZone";
                ALTER TABLE "TenantSignups" DROP COLUMN IF EXISTS "TimeZone";
                ALTER TABLE "StudentProfiles" DROP COLUMN IF EXISTS "Gender";
                ALTER TABLE "StudentProfiles" DROP COLUMN IF EXISTS "TimeZone";
                ALTER TABLE "StudentProfiles" DROP COLUMN IF EXISTS "SessionMinutes";
                ALTER TABLE "StudentProfiles" DROP COLUMN IF EXISTS "SessionsPerWeek";
                ALTER TABLE "TeacherProfiles" DROP COLUMN IF EXISTS "Gender";
                ALTER TABLE "TeacherProfiles" DROP COLUMN IF EXISTS "TimeZone";
                """);
        }
    }
}
