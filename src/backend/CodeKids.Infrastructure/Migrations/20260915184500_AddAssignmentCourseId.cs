using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260915184500_AddAssignmentCourseId")]
    public class AddAssignmentCourseId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Assignments" ADD COLUMN IF NOT EXISTS "CourseId" uuid NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Assignments_Courses_CourseId'
                    ) THEN
                        ALTER TABLE "Assignments"
                            ADD CONSTRAINT "FK_Assignments_Courses_CourseId"
                            FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE SET NULL;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_Assignments_CourseId" ON "Assignments" ("CourseId");

                -- Unique teacher+classroom course, then unique classroom course — never a random leftover subject.
                UPDATE "Assignments" a
                SET "CourseId" = matched."CourseId"
                FROM (
                    SELECT cc."ClassroomId", cc."TeacherId", (array_agg(cc."CourseId" ORDER BY cc."CourseId"))[1] AS "CourseId"
                    FROM "ClassroomCourses" cc
                    GROUP BY cc."ClassroomId", cc."TeacherId"
                    HAVING COUNT(DISTINCT cc."CourseId") = 1
                ) matched
                WHERE a."CourseId" IS NULL
                  AND a."ClassroomId" = matched."ClassroomId"
                  AND a."CreatedByUserId" = matched."TeacherId";

                UPDATE "Assignments" a
                SET "CourseId" = matched."CourseId"
                FROM (
                    SELECT cc."ClassroomId", (array_agg(cc."CourseId" ORDER BY cc."CourseId"))[1] AS "CourseId"
                    FROM "ClassroomCourses" cc
                    GROUP BY cc."ClassroomId"
                    HAVING COUNT(DISTINCT cc."CourseId") = 1
                ) matched
                WHERE a."CourseId" IS NULL
                  AND a."ClassroomId" = matched."ClassroomId";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Assignments" DROP CONSTRAINT IF EXISTS "FK_Assignments_Courses_CourseId";
                DROP INDEX IF EXISTS "IX_Assignments_CourseId";
                ALTER TABLE "Assignments" DROP COLUMN IF EXISTS "CourseId";
                """);
        }
    }
}
