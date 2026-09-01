using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260901150000_SetAssessmentIsPublishedDefaultFalse")]
    public class SetAssessmentIsPublishedDefaultFalse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Assignments" ALTER COLUMN "IsPublished" SET DEFAULT FALSE;
                ALTER TABLE "Quizzes" ALTER COLUMN "IsPublished" SET DEFAULT FALSE;
                ALTER TABLE "Exams" ALTER COLUMN "IsPublished" SET DEFAULT FALSE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Assignments" ALTER COLUMN "IsPublished" SET DEFAULT TRUE;
                ALTER TABLE "Quizzes" ALTER COLUMN "IsPublished" SET DEFAULT TRUE;
                ALTER TABLE "Exams" ALTER COLUMN "IsPublished" SET DEFAULT TRUE;
                """);
        }
    }
}
