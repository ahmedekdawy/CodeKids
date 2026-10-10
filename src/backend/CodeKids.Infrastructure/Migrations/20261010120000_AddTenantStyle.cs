using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261010120000_AddTenantStyle")]
    public class AddTenantStyle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "TenantSignups" ADD COLUMN IF NOT EXISTS "TenantStyle" character varying(20) NOT NULL DEFAULT 'School';
                ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "TenantStyle" character varying(20) NOT NULL DEFAULT 'School';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "TenantSignups" DROP COLUMN IF EXISTS "TenantStyle";
                ALTER TABLE "SiteSettings" DROP COLUMN IF EXISTS "TenantStyle";
                """);
        }
    }
}
