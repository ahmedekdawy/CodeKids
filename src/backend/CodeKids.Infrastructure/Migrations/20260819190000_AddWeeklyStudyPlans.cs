using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260819190000_AddWeeklyStudyPlans")]
    public class AddWeeklyStudyPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "WeeklyStudyPlans" (
                    "Id" uuid NOT NULL,
                    "TeacherId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "FromDate" date NOT NULL,
                    "ToDate" date NOT NULL,
                    "Notes" character varying(1000) NOT NULL DEFAULT '',
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_WeeklyStudyPlans" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "WeeklyStudyPlanItems" (
                    "Id" uuid NOT NULL,
                    "WeeklyStudyPlanId" uuid NOT NULL,
                    "ItemDate" date NOT NULL,
                    "Topic" character varying(300) NOT NULL DEFAULT '',
                    "Classwork" character varying(500) NOT NULL DEFAULT '',
                    "Homework" character varying(500) NOT NULL DEFAULT '',
                    "Notes" character varying(500) NOT NULL DEFAULT '',
                    "SortOrder" integer NOT NULL DEFAULT 0,
                    CONSTRAINT "PK_WeeklyStudyPlanItems" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_WeeklyStudyPlans_Users_TeacherId'
                    ) THEN
                        ALTER TABLE "WeeklyStudyPlans"
                            ADD CONSTRAINT "FK_WeeklyStudyPlans_Users_TeacherId"
                            FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_WeeklyStudyPlans_Courses_CourseId'
                    ) THEN
                        ALTER TABLE "WeeklyStudyPlans"
                            ADD CONSTRAINT "FK_WeeklyStudyPlans_Courses_CourseId"
                            FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_WeeklyStudyPlanItems_WeeklyStudyPlans_WeeklyStudyPlanId'
                    ) THEN
                        ALTER TABLE "WeeklyStudyPlanItems"
                            ADD CONSTRAINT "FK_WeeklyStudyPlanItems_WeeklyStudyPlans_WeeklyStudyPlanId"
                            FOREIGN KEY ("WeeklyStudyPlanId") REFERENCES "WeeklyStudyPlans" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_WeeklyStudyPlans_TeacherId_CourseId_FromDate"
                    ON "WeeklyStudyPlans" ("TeacherId", "CourseId", "FromDate");
                CREATE INDEX IF NOT EXISTS "IX_WeeklyStudyPlans_FromDate"
                    ON "WeeklyStudyPlans" ("FromDate");
                CREATE INDEX IF NOT EXISTS "IX_WeeklyStudyPlans_ToDate"
                    ON "WeeklyStudyPlans" ("ToDate");
                CREATE INDEX IF NOT EXISTS "IX_WeeklyStudyPlans_CourseId"
                    ON "WeeklyStudyPlans" ("CourseId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_WeeklyStudyPlanItems_WeeklyStudyPlanId_ItemDate"
                    ON "WeeklyStudyPlanItems" ("WeeklyStudyPlanId", "ItemDate");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "WeeklyStudyPlanItems";
                DROP TABLE IF EXISTS "WeeklyStudyPlans";
                """);
        }
    }
}
