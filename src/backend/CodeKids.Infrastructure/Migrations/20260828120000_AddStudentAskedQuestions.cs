using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260828120000_AddStudentAskedQuestions")]
    public class AddStudentAskedQuestions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "StudentAskedQuestions" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64),
                    "StudentId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "UnitId" uuid,
                    "LessonId" uuid,
                    "CourseTitle" character varying(300) NOT NULL DEFAULT '',
                    "UnitTitle" character varying(300) NOT NULL DEFAULT '',
                    "LessonTitle" character varying(300) NOT NULL DEFAULT '',
                    "StudentName" character varying(80) NOT NULL DEFAULT '',
                    "Question" character varying(800) NOT NULL,
                    "AiAnswer" text NOT NULL DEFAULT '',
                    "AiInScope" boolean NOT NULL DEFAULT FALSE,
                    "TeacherAnswer" text NOT NULL DEFAULT '',
                    "TeacherId" uuid,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "TeacherAnsweredAtUtc" timestamp with time zone,
                    CONSTRAINT "PK_StudentAskedQuestions" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_StudentAskedQuestions_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_StudentAskedQuestions_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_StudentAskedQuestions_Users_TeacherId" FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE SET NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_StudentAskedQuestions_TenantId" ON "StudentAskedQuestions" ("TenantId");
                CREATE INDEX IF NOT EXISTS "IX_StudentAskedQuestions_StudentId" ON "StudentAskedQuestions" ("StudentId");
                CREATE INDEX IF NOT EXISTS "IX_StudentAskedQuestions_CourseId_CreatedAtUtc" ON "StudentAskedQuestions" ("CourseId", "CreatedAtUtc");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "StudentAskedQuestions";""");
        }
    }
}
