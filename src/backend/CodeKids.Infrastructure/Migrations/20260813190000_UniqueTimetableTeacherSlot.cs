using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueTimetableTeacherSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber_CourseId";
                DROP INDEX IF EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber";
                DROP INDEX IF EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNum~";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber"
                    ON "FixedTimetableEntries" ("TeacherId", "DayOfWeek", "Period", "SessionNumber");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber_CourseId"
                    ON "FixedTimetableEntries" ("TeacherId", "DayOfWeek", "Period", "SessionNumber", "CourseId");
                """);
        }
    }
}
