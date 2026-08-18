using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260818223000_AddQuizCreatedAtAndAttemptAnswers")]
    public class AddQuizCreatedAtAndAttemptAnswers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Quizzes"
                    ADD COLUMN IF NOT EXISTS "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT now();

                CREATE TABLE IF NOT EXISTS "QuizAttemptAnswers" (
                    "Id" uuid NOT NULL,
                    "AttemptId" uuid NOT NULL,
                    "QuestionId" uuid NOT NULL,
                    "SelectedOption" character varying(40) NOT NULL DEFAULT '',
                    "IsCorrect" boolean NOT NULL,
                    CONSTRAINT "PK_QuizAttemptAnswers" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_QuizAttemptAnswers_QuizAttempts_AttemptId'
                    ) THEN
                        ALTER TABLE "QuizAttemptAnswers"
                            ADD CONSTRAINT "FK_QuizAttemptAnswers_QuizAttempts_AttemptId"
                            FOREIGN KEY ("AttemptId") REFERENCES "QuizAttempts" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_QuizAttemptAnswers_QuizQuestions_QuestionId'
                    ) THEN
                        ALTER TABLE "QuizAttemptAnswers"
                            ADD CONSTRAINT "FK_QuizAttemptAnswers_QuizQuestions_QuestionId"
                            FOREIGN KEY ("QuestionId") REFERENCES "QuizQuestions" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_QuizAttemptAnswers_AttemptId_QuestionId"
                    ON "QuizAttemptAnswers" ("AttemptId", "QuestionId");
                CREATE INDEX IF NOT EXISTS "IX_QuizAttemptAnswers_QuestionId"
                    ON "QuizAttemptAnswers" ("QuestionId");
                CREATE INDEX IF NOT EXISTS "IX_Quizzes_CreatedAtUtc"
                    ON "Quizzes" ("CreatedAtUtc");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "QuizAttemptAnswers";
                DROP INDEX IF EXISTS "IX_Quizzes_CreatedAtUtc";
                ALTER TABLE "Quizzes" DROP COLUMN IF EXISTS "CreatedAtUtc";
                """);
        }
    }
}
