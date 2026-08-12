using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedTimetableEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "FixedTimetableEntries" (
                    "Id" uuid NOT NULL,
                    "TeacherId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "DayOfWeek" integer NOT NULL,
                    "SessionNumber" integer NOT NULL,
                    "Period" character varying(10) NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_FixedTimetableEntries" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_FixedTimetableEntries_Users_TeacherId'
                    ) THEN
                        ALTER TABLE "FixedTimetableEntries"
                            ADD CONSTRAINT "FK_FixedTimetableEntries_Users_TeacherId"
                            FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_FixedTimetableEntries_Courses_CourseId'
                    ) THEN
                        ALTER TABLE "FixedTimetableEntries"
                            ADD CONSTRAINT "FK_FixedTimetableEntries_Courses_CourseId"
                            FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNum~"
                    ON "FixedTimetableEntries" ("TeacherId", "DayOfWeek", "Period", "SessionNumber");

                CREATE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_DayOfWeek_Period_SessionNumber"
                    ON "FixedTimetableEntries" ("DayOfWeek", "Period", "SessionNumber");

                CREATE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_CourseId"
                    ON "FixedTimetableEntries" ("CourseId");

                CREATE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_TeacherId"
                    ON "FixedTimetableEntries" ("TeacherId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "FixedTimetableEntries";""");
        }
    }
}
