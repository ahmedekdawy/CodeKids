using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260826220000_AddPmStartMinutes")]
    public class AddPmStartMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "SiteSettings"
                    ADD COLUMN IF NOT EXISTS "PmStartMinutes" integer NOT NULL DEFAULT 900;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "SiteSettings" DROP COLUMN IF EXISTS "PmStartMinutes";
                """);
        }
    }
}
