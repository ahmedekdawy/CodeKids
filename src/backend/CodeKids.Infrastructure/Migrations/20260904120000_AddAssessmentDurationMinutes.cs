using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260904120000_AddAssessmentDurationMinutes")]
    public class AddAssessmentDurationMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Quizzes" ADD COLUMN IF NOT EXISTS "DurationMinutes" integer NULL;
                ALTER TABLE "Exams" ADD COLUMN IF NOT EXISTS "DurationMinutes" integer NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Exams" DROP COLUMN IF EXISTS "DurationMinutes";
                ALTER TABLE "Quizzes" DROP COLUMN IF EXISTS "DurationMinutes";
                """);
        }
    }
}
