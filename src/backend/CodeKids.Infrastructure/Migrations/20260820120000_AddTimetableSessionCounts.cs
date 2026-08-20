using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260820120000_AddTimetableSessionCounts")]
    public class AddTimetableSessionCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "SiteSettings"
                    ADD COLUMN IF NOT EXISTS "AmSessionCount" integer NOT NULL DEFAULT 6;
                ALTER TABLE "SiteSettings"
                    ADD COLUMN IF NOT EXISTS "PmSessionCount" integer NOT NULL DEFAULT 6;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "SiteSettings" DROP COLUMN IF EXISTS "AmSessionCount";
                ALTER TABLE "SiteSettings" DROP COLUMN IF EXISTS "PmSessionCount";
                """);
        }
    }
}
