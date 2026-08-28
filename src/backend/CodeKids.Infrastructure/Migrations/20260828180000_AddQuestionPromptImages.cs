using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260828180000_AddQuestionPromptImages")]
    public class AddQuestionPromptImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "BankQuestions" ADD COLUMN IF NOT EXISTS "PromptImageMediaAssetId" uuid NULL;
                ALTER TABLE "ExamQuestions" ADD COLUMN IF NOT EXISTS "PromptImageMediaAssetId" uuid NULL;
                ALTER TABLE "QuizQuestions" ADD COLUMN IF NOT EXISTS "PromptImageMediaAssetId" uuid NULL;
                ALTER TABLE "AssignmentQuestions" ADD COLUMN IF NOT EXISTS "PromptImageMediaAssetId" uuid NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_BankQuestions_MediaAssets_PromptImageMediaAssetId'
                    ) THEN
                        ALTER TABLE "BankQuestions"
                            ADD CONSTRAINT "FK_BankQuestions_MediaAssets_PromptImageMediaAssetId"
                            FOREIGN KEY ("PromptImageMediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_ExamQuestions_MediaAssets_PromptImageMediaAssetId'
                    ) THEN
                        ALTER TABLE "ExamQuestions"
                            ADD CONSTRAINT "FK_ExamQuestions_MediaAssets_PromptImageMediaAssetId"
                            FOREIGN KEY ("PromptImageMediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_QuizQuestions_MediaAssets_PromptImageMediaAssetId'
                    ) THEN
                        ALTER TABLE "QuizQuestions"
                            ADD CONSTRAINT "FK_QuizQuestions_MediaAssets_PromptImageMediaAssetId"
                            FOREIGN KEY ("PromptImageMediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_AssignmentQuestions_MediaAssets_PromptImageMediaAssetId'
                    ) THEN
                        ALTER TABLE "AssignmentQuestions"
                            ADD CONSTRAINT "FK_AssignmentQuestions_MediaAssets_PromptImageMediaAssetId"
                            FOREIGN KEY ("PromptImageMediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "BankQuestions" DROP CONSTRAINT IF EXISTS "FK_BankQuestions_MediaAssets_PromptImageMediaAssetId";
                ALTER TABLE "ExamQuestions" DROP CONSTRAINT IF EXISTS "FK_ExamQuestions_MediaAssets_PromptImageMediaAssetId";
                ALTER TABLE "QuizQuestions" DROP CONSTRAINT IF EXISTS "FK_QuizQuestions_MediaAssets_PromptImageMediaAssetId";
                ALTER TABLE "AssignmentQuestions" DROP CONSTRAINT IF EXISTS "FK_AssignmentQuestions_MediaAssets_PromptImageMediaAssetId";

                ALTER TABLE "BankQuestions" DROP COLUMN IF EXISTS "PromptImageMediaAssetId";
                ALTER TABLE "ExamQuestions" DROP COLUMN IF EXISTS "PromptImageMediaAssetId";
                ALTER TABLE "QuizQuestions" DROP COLUMN IF EXISTS "PromptImageMediaAssetId";
                ALTER TABLE "AssignmentQuestions" DROP COLUMN IF EXISTS "PromptImageMediaAssetId";
                """);
        }
    }
}
