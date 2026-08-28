using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260827120000_DropCourseUnitsAndLessons")]
    public class DropCourseUnitsAndLessons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "SubjectUnits" ADD COLUMN IF NOT EXISTS "StudentAskEnabled" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE "SubjectUnitLessons" ADD COLUMN IF NOT EXISTS "StudentAskEnabled" boolean NOT NULL DEFAULT FALSE;

                ALTER TABLE "LessonSteps" DROP CONSTRAINT IF EXISTS "FK_LessonSteps_Lessons_LessonId";
                ALTER TABLE "LessonVideos" DROP CONSTRAINT IF EXISTS "FK_LessonVideos_Lessons_LessonId";
                ALTER TABLE "BankQuestions" DROP CONSTRAINT IF EXISTS "FK_BankQuestions_Lessons_LessonId";
                ALTER TABLE "ExamQuestions" DROP CONSTRAINT IF EXISTS "FK_ExamQuestions_Lessons_LessonId";
                ALTER TABLE "VideoWatchSessions" DROP CONSTRAINT IF EXISTS "FK_VideoWatchSessions_Lessons_LessonId";
                ALTER TABLE "Lessons" DROP CONSTRAINT IF EXISTS "FK_Lessons_CourseUnits_UnitId";
                ALTER TABLE "Lessons" DROP CONSTRAINT IF EXISTS "FK_Lessons_Courses_CourseId";
                ALTER TABLE "CourseUnits" DROP CONSTRAINT IF EXISTS "FK_CourseUnits_Courses_CourseId";

                DROP TABLE IF EXISTS "Lessons";
                DROP TABLE IF EXISTS "CourseUnits";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
