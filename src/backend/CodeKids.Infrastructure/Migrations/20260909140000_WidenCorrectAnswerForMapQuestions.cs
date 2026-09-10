using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260909140000_WidenCorrectAnswerForMapQuestions")]
    public class WidenCorrectAnswerForMapQuestions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "BankQuestions" ALTER COLUMN "CorrectAnswer" TYPE character varying(4000);
                ALTER TABLE "ExamQuestions" ALTER COLUMN "CorrectAnswer" TYPE character varying(4000);
                ALTER TABLE "QuizQuestions" ALTER COLUMN "CorrectAnswer" TYPE character varying(4000);
                ALTER TABLE "AssignmentQuestions" ALTER COLUMN "CorrectAnswer" TYPE character varying(4000);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "BankQuestions" ALTER COLUMN "CorrectAnswer" TYPE character varying(200);
                ALTER TABLE "ExamQuestions" ALTER COLUMN "CorrectAnswer" TYPE character varying(200);
                ALTER TABLE "QuizQuestions" ALTER COLUMN "CorrectAnswer" TYPE character varying(500);
                ALTER TABLE "AssignmentQuestions" ALTER COLUMN "CorrectAnswer" TYPE character varying(500);
                """);
        }
    }
}
