using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260826140000_AddStudentAskEnabled")]
    public class AddStudentAskEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "StudentAskEnabled" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE "CourseUnits" ADD COLUMN IF NOT EXISTS "StudentAskEnabled" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE "Lessons" ADD COLUMN IF NOT EXISTS "StudentAskEnabled" boolean NOT NULL DEFAULT FALSE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "StudentAskEnabled";
                ALTER TABLE "CourseUnits" DROP COLUMN IF EXISTS "StudentAskEnabled";
                ALTER TABLE "Lessons" DROP COLUMN IF EXISTS "StudentAskEnabled";
                """);
        }
    }
}
