using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260818180000_AddStudentWeeklyReports")]
    public class AddStudentWeeklyReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "StudentWeeklyReports" (
                    "Id" uuid NOT NULL,
                    "TeacherId" uuid NOT NULL,
                    "StudentId" uuid NOT NULL,
                    "WeekStartDate" date NOT NULL,
                    "PerformancePercent" integer NULL,
                    "AttendancePercent" integer NULL,
                    "HomeworkPercent" integer NULL,
                    "InteractionDuringSession" character varying(200) NOT NULL DEFAULT '',
                    "OpenCamera" boolean NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_StudentWeeklyReports" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentWeeklyReports_Users_TeacherId'
                    ) THEN
                        ALTER TABLE "StudentWeeklyReports"
                            ADD CONSTRAINT "FK_StudentWeeklyReports_Users_TeacherId"
                            FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentWeeklyReports_Users_StudentId'
                    ) THEN
                        ALTER TABLE "StudentWeeklyReports"
                            ADD CONSTRAINT "FK_StudentWeeklyReports_Users_StudentId"
                            FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_StudentWeeklyReports_TeacherId_StudentId_WeekStartDate"
                    ON "StudentWeeklyReports" ("TeacherId", "StudentId", "WeekStartDate");
                CREATE INDEX IF NOT EXISTS "IX_StudentWeeklyReports_WeekStartDate"
                    ON "StudentWeeklyReports" ("WeekStartDate");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "StudentWeeklyReports";
                """);
        }
    }
}
