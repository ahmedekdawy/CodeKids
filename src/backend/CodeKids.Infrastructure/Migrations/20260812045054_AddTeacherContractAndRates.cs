using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherContractAndRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ContractType" character varying(20) NULL;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PrimaryAmount" numeric(18,2) NULL;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PrepAmount" numeric(18,2) NULL;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SecondaryAmount" numeric(18,2) NULL;

                CREATE TABLE IF NOT EXISTS "TeacherCourseRates" (
                    "Id" uuid NOT NULL,
                    "TeacherId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "SessionAmount" numeric(18,2) NULL,
                    "MonthlySalary" numeric(18,2) NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_TeacherCourseRates" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_TeacherCourseRates_Users_TeacherId'
                    ) THEN
                        ALTER TABLE "TeacherCourseRates"
                            ADD CONSTRAINT "FK_TeacherCourseRates_Users_TeacherId"
                            FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE CASCADE;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_TeacherCourseRates_Courses_CourseId'
                    ) THEN
                        ALTER TABLE "TeacherCourseRates"
                            ADD CONSTRAINT "FK_TeacherCourseRates_Courses_CourseId"
                            FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TeacherCourseRates_TeacherId_CourseId"
                    ON "TeacherCourseRates" ("TeacherId", "CourseId");
                CREATE INDEX IF NOT EXISTS "IX_TeacherCourseRates_CourseId"
                    ON "TeacherCourseRates" ("CourseId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "TeacherCourseRates";
                ALTER TABLE "Users" DROP COLUMN IF EXISTS "ContractType";
                ALTER TABLE "Users" DROP COLUMN IF EXISTS "PrimaryAmount";
                ALTER TABLE "Users" DROP COLUMN IF EXISTS "PrepAmount";
                ALTER TABLE "Users" DROP COLUMN IF EXISTS "SecondaryAmount";
                """);
        }
    }
}
