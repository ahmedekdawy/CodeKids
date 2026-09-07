using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260830120000_AddSubmissionAnswerImages")]
    public class AddSubmissionAnswerImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "AssignmentAnswers" ADD COLUMN IF NOT EXISTS "AnswerImageMediaAssetId" uuid NULL;
                ALTER TABLE "AssignmentSubmissions" ADD COLUMN IF NOT EXISTS "FeedbackImageMediaAssetId" uuid NULL;
                ALTER TABLE "ExamAnswers" ADD COLUMN IF NOT EXISTS "AnswerImageMediaAssetId" uuid NULL;
                ALTER TABLE "ExamAttempts" ADD COLUMN IF NOT EXISTS "FeedbackImageMediaAssetId" uuid NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_AssignmentAnswers_MediaAssets_AnswerImageMediaAssetId'
                    ) THEN
                        ALTER TABLE "AssignmentAnswers"
                            ADD CONSTRAINT "FK_AssignmentAnswers_MediaAssets_AnswerImageMediaAssetId"
                            FOREIGN KEY ("AnswerImageMediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_AssignmentSubmissions_MediaAssets_FeedbackImageMediaAssetId'
                    ) THEN
                        ALTER TABLE "AssignmentSubmissions"
                            ADD CONSTRAINT "FK_AssignmentSubmissions_MediaAssets_FeedbackImageMediaAssetId"
                            FOREIGN KEY ("FeedbackImageMediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_ExamAnswers_MediaAssets_AnswerImageMediaAssetId'
                    ) THEN
                        ALTER TABLE "ExamAnswers"
                            ADD CONSTRAINT "FK_ExamAnswers_MediaAssets_AnswerImageMediaAssetId"
                            FOREIGN KEY ("AnswerImageMediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_ExamAttempts_MediaAssets_FeedbackImageMediaAssetId'
                    ) THEN
                        ALTER TABLE "ExamAttempts"
                            ADD CONSTRAINT "FK_ExamAttempts_MediaAssets_FeedbackImageMediaAssetId"
                            FOREIGN KEY ("FeedbackImageMediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "AssignmentAnswers" DROP CONSTRAINT IF EXISTS "FK_AssignmentAnswers_MediaAssets_AnswerImageMediaAssetId";
                ALTER TABLE "AssignmentSubmissions" DROP CONSTRAINT IF EXISTS "FK_AssignmentSubmissions_MediaAssets_FeedbackImageMediaAssetId";
                ALTER TABLE "ExamAnswers" DROP CONSTRAINT IF EXISTS "FK_ExamAnswers_MediaAssets_AnswerImageMediaAssetId";
                ALTER TABLE "ExamAttempts" DROP CONSTRAINT IF EXISTS "FK_ExamAttempts_MediaAssets_FeedbackImageMediaAssetId";

                ALTER TABLE "AssignmentAnswers" DROP COLUMN IF EXISTS "AnswerImageMediaAssetId";
                ALTER TABLE "AssignmentSubmissions" DROP COLUMN IF EXISTS "FeedbackImageMediaAssetId";
                ALTER TABLE "ExamAnswers" DROP COLUMN IF EXISTS "AnswerImageMediaAssetId";
                ALTER TABLE "ExamAttempts" DROP COLUMN IF EXISTS "FeedbackImageMediaAssetId";
                """);
        }
    }
}
