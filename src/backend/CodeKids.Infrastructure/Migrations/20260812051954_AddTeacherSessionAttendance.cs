using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherSessionAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "TeacherSessionAttendances" (
                    "Id" uuid NOT NULL,
                    "TeacherId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "SessionDate" date NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_TeacherSessionAttendances" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_TeacherSessionAttendances_Users_TeacherId'
                    ) THEN
                        ALTER TABLE "TeacherSessionAttendances"
                            ADD CONSTRAINT "FK_TeacherSessionAttendances_Users_TeacherId"
                            FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_TeacherSessionAttendances_Courses_CourseId'
                    ) THEN
                        ALTER TABLE "TeacherSessionAttendances"
                            ADD CONSTRAINT "FK_TeacherSessionAttendances_Courses_CourseId"
                            FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TeacherSessionAttendances_TeacherId_CourseId_SessionDate"
                    ON "TeacherSessionAttendances" ("TeacherId", "CourseId", "SessionDate");
                CREATE INDEX IF NOT EXISTS "IX_TeacherSessionAttendances_SessionDate"
                    ON "TeacherSessionAttendances" ("SessionDate");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "TeacherSessionAttendances";
                """);
        }
    }
}
