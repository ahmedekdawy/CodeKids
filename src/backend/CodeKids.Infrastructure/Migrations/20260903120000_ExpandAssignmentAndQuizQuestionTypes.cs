using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260903120000_ExpandAssignmentAndQuizQuestionTypes")]
    public class ExpandAssignmentAndQuizQuestionTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "AssignmentQuestions" ADD COLUMN IF NOT EXISTS "PassageText" character varying(4000) NOT NULL DEFAULT '';
                ALTER TABLE "AssignmentQuestions" ADD COLUMN IF NOT EXISTS "OptionsJson" character varying(8000) NOT NULL DEFAULT '[]';
                ALTER TABLE "AssignmentQuestions" ADD COLUMN IF NOT EXISTS "ParentQuestionId" uuid NULL;
                ALTER TABLE "AssignmentQuestions" ALTER COLUMN "Prompt" TYPE character varying(4000);
                ALTER TABLE "AssignmentQuestions" ALTER COLUMN "CorrectAnswer" TYPE character varying(500);
                ALTER TABLE "AssignmentQuestions" ALTER COLUMN "OptionA" TYPE character varying(200);
                ALTER TABLE "AssignmentQuestions" ALTER COLUMN "OptionB" TYPE character varying(200);
                ALTER TABLE "AssignmentQuestions" ALTER COLUMN "OptionC" TYPE character varying(200);

                ALTER TABLE "QuizQuestions" ADD COLUMN IF NOT EXISTS "QuestionType" character varying(30) NOT NULL DEFAULT 'SingleChoice';
                ALTER TABLE "QuizQuestions" ADD COLUMN IF NOT EXISTS "PassageText" character varying(4000) NOT NULL DEFAULT '';
                ALTER TABLE "QuizQuestions" ADD COLUMN IF NOT EXISTS "CorrectAnswer" character varying(500) NOT NULL DEFAULT '';
                ALTER TABLE "QuizQuestions" ADD COLUMN IF NOT EXISTS "Points" integer NOT NULL DEFAULT 1;
                ALTER TABLE "QuizQuestions" ADD COLUMN IF NOT EXISTS "ParentQuestionId" uuid NULL;
                ALTER TABLE "QuizQuestions" ALTER COLUMN "Prompt" TYPE character varying(4000);
                ALTER TABLE "QuizQuestions" ALTER COLUMN "CorrectOption" TYPE character varying(500);
                ALTER TABLE "QuizAttemptAnswers" ALTER COLUMN "SelectedOption" TYPE character varying(1000);

                UPDATE "QuizQuestions"
                SET "CorrectAnswer" = "CorrectOption"
                WHERE COALESCE("CorrectAnswer", '') = '' AND COALESCE("CorrectOption", '') <> '';

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_AssignmentQuestions_AssignmentQuestions_ParentQuestionId'
                    ) THEN
                        ALTER TABLE "AssignmentQuestions"
                            ADD CONSTRAINT "FK_AssignmentQuestions_AssignmentQuestions_ParentQuestionId"
                            FOREIGN KEY ("ParentQuestionId") REFERENCES "AssignmentQuestions" ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_QuizQuestions_QuizQuestions_ParentQuestionId'
                    ) THEN
                        ALTER TABLE "QuizQuestions"
                            ADD CONSTRAINT "FK_QuizQuestions_QuizQuestions_ParentQuestionId"
                            FOREIGN KEY ("ParentQuestionId") REFERENCES "QuizQuestions" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "AssignmentQuestions" DROP CONSTRAINT IF EXISTS "FK_AssignmentQuestions_AssignmentQuestions_ParentQuestionId";
                ALTER TABLE "QuizQuestions" DROP CONSTRAINT IF EXISTS "FK_QuizQuestions_QuizQuestions_ParentQuestionId";

                ALTER TABLE "AssignmentQuestions" DROP COLUMN IF EXISTS "PassageText";
                ALTER TABLE "AssignmentQuestions" DROP COLUMN IF EXISTS "OptionsJson";
                ALTER TABLE "AssignmentQuestions" DROP COLUMN IF EXISTS "ParentQuestionId";

                ALTER TABLE "QuizQuestions" DROP COLUMN IF EXISTS "QuestionType";
                ALTER TABLE "QuizQuestions" DROP COLUMN IF EXISTS "PassageText";
                ALTER TABLE "QuizQuestions" DROP COLUMN IF EXISTS "CorrectAnswer";
                ALTER TABLE "QuizQuestions" DROP COLUMN IF EXISTS "Points";
                ALTER TABLE "QuizQuestions" DROP COLUMN IF EXISTS "ParentQuestionId";
                """);
        }
    }
}
