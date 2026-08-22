using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260816220000_AddTenantSignupMobilePhone")]
    public partial class AddTenantSignupMobilePhone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "TenantSignups"
                    ADD COLUMN IF NOT EXISTS "MobilePhone" character varying(30) NOT NULL DEFAULT '';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "TenantSignups" DROP COLUMN IF EXISTS "MobilePhone";
                """);
        }
    }
}
