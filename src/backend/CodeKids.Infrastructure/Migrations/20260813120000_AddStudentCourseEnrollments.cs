using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentCourseEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "StudentCourseEnrollments" (
                    "Id" uuid NOT NULL,
                    "StudentId" uuid NOT NULL,
                    "ClassroomId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "EnrolledAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_StudentCourseEnrollments" PRIMARY KEY ("Id")
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_StudentCourseEnrollments_StudentId_ClassroomId_CourseId"
                    ON "StudentCourseEnrollments" ("StudentId", "ClassroomId", "CourseId");
                CREATE INDEX IF NOT EXISTS "IX_StudentCourseEnrollments_ClassroomId"
                    ON "StudentCourseEnrollments" ("ClassroomId");
                CREATE INDEX IF NOT EXISTS "IX_StudentCourseEnrollments_CourseId"
                    ON "StudentCourseEnrollments" ("CourseId");
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentCourseEnrollments_Users_StudentId'
                    ) THEN
                        ALTER TABLE "StudentCourseEnrollments"
                            ADD CONSTRAINT "FK_StudentCourseEnrollments_Users_StudentId"
                            FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE CASCADE;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentCourseEnrollments_Classrooms_ClassroomId'
                    ) THEN
                        ALTER TABLE "StudentCourseEnrollments"
                            ADD CONSTRAINT "FK_StudentCourseEnrollments_Classrooms_ClassroomId"
                            FOREIGN KEY ("ClassroomId") REFERENCES "Classrooms" ("Id") ON DELETE CASCADE;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentCourseEnrollments_Courses_CourseId'
                    ) THEN
                        ALTER TABLE "StudentCourseEnrollments"
                            ADD CONSTRAINT "FK_StudentCourseEnrollments_Courses_CourseId"
                            FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "StudentCourseEnrollments" DROP CONSTRAINT IF EXISTS "FK_StudentCourseEnrollments_Users_StudentId";
                ALTER TABLE "StudentCourseEnrollments" DROP CONSTRAINT IF EXISTS "FK_StudentCourseEnrollments_Classrooms_ClassroomId";
                ALTER TABLE "StudentCourseEnrollments" DROP CONSTRAINT IF EXISTS "FK_StudentCourseEnrollments_Courses_CourseId";
                DROP TABLE IF EXISTS "StudentCourseEnrollments";
                """);
        }
    }
}
