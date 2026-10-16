using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <summary>Student answers are written in a rich text editor now; its HTML needs more room than plain text.</summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261016120000_WidenAnswerText")]
    public class WidenAnswerText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "AssignmentAnswers" ALTER COLUMN "AnswerText" TYPE character varying(8000);
                ALTER TABLE "ExamAnswers" ALTER COLUMN "AnswerText" TYPE character varying(8000);
                ALTER TABLE "QuizAttemptAnswers" ALTER COLUMN "SelectedOption" TYPE character varying(8000);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Narrowing would fail on answers longer than 1000 characters, so the columns stay wide.
        }
    }
}
