using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261108120000_AddAmStartMinutes")]
    public class AddAmStartMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows keep the 08:00 first lesson the timetable was hard-coded to.
            migrationBuilder.Sql(
                """
                ALTER TABLE "SiteSettings"
                    ADD COLUMN IF NOT EXISTS "AmStartMinutes" integer NOT NULL DEFAULT 480;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "SiteSettings" DROP COLUMN IF EXISTS "AmStartMinutes";
                """);
        }
    }
}
