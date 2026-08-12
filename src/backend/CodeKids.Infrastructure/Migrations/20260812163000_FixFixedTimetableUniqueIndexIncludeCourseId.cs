using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFixedTimetableUniqueIndexIncludeCourseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber";
                DROP INDEX IF EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNum~";

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber_CourseId"
                    ON "FixedTimetableEntries" ("TeacherId", "DayOfWeek", "Period", "SessionNumber", "CourseId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber_CourseId";

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber"
                    ON "FixedTimetableEntries" ("TeacherId", "DayOfWeek", "Period", "SessionNumber");
                """);
        }
    }
}
