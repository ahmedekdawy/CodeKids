using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260822120000_AddTimetableCombinedGrades")]
    public class AddTimetableCombinedGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "FixedTimetableEntries"
                    ADD COLUMN IF NOT EXISTS "CombinedGrades" character varying(80) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "FixedTimetableEntries" DROP COLUMN IF EXISTS "CombinedGrades";
                """);
        }
    }
}
