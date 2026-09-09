using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260909120000_AddQuizAttemptAnswerImages")]
    public class AddQuizAttemptAnswerImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "QuizAttemptAnswers" ADD COLUMN IF NOT EXISTS "AnswerImageMediaAssetId" uuid NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_QuizAttemptAnswers_MediaAssets_AnswerImageMediaAssetId'
                    ) THEN
                        ALTER TABLE "QuizAttemptAnswers"
                            ADD CONSTRAINT "FK_QuizAttemptAnswers_MediaAssets_AnswerImageMediaAssetId"
                            FOREIGN KEY ("AnswerImageMediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "QuizAttemptAnswers" DROP CONSTRAINT IF EXISTS "FK_QuizAttemptAnswers_MediaAssets_AnswerImageMediaAssetId";
                ALTER TABLE "QuizAttemptAnswers" DROP COLUMN IF EXISTS "AnswerImageMediaAssetId";
                """);
        }
    }
}
