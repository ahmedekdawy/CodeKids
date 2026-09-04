using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260905000000_AddCourseIsPublished")]
    public class AddCourseIsPublished : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing courses stay visible; new ones start as drafts.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "IsPublished" boolean NOT NULL DEFAULT TRUE;
                ALTER TABLE "Courses" ALTER COLUMN "IsPublished" SET DEFAULT FALSE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "IsPublished";
                """);
        }
    }
}
