using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260831233000_MoveCourseVideosToLessonVideos")]
    public class MoveCourseVideosToLessonVideos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "LessonVideos" ADD COLUMN IF NOT EXISTS "CourseId" uuid;
                ALTER TABLE "LessonVideos" ALTER COLUMN "LessonId" DROP NOT NULL;

                CREATE INDEX IF NOT EXISTS "IX_LessonVideos_CourseId" ON "LessonVideos" ("CourseId");

                DO $fk$
                BEGIN
                  IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_LessonVideos_Courses_CourseId'
                  ) THEN
                    ALTER TABLE "LessonVideos"
                    ADD CONSTRAINT "FK_LessonVideos_Courses_CourseId"
                    FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE;
                  END IF;
                END
                $fk$;

                DO $move$
                BEGIN
                  IF EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'CourseVideos'
                  ) THEN
                    EXECUTE $sql$
                      INSERT INTO "LessonVideos" (
                          "Id", "TenantId", "LessonId", "CourseId", "MediaAssetId", "Title", "SortOrder", "CreatedAtUtc"
                      )
                      SELECT
                          cv."Id",
                          cv."TenantId",
                          NULL,
                          cv."CourseId",
                          cv."MediaAssetId",
                          cv."Title",
                          cv."SortOrder",
                          cv."CreatedAtUtc"
                      FROM "CourseVideos" cv
                      WHERE NOT EXISTS (
                          SELECT 1 FROM "LessonVideos" lv WHERE lv."Id" = cv."Id"
                      )
                    $sql$;
                  END IF;
                END
                $move$;

                DROP TABLE IF EXISTS "CourseVideos" CASCADE;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "CourseVideos" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64),
                    "CourseId" uuid NOT NULL,
                    "MediaAssetId" uuid NOT NULL,
                    "Title" character varying(160) NOT NULL,
                    "SortOrder" integer NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_CourseVideos" PRIMARY KEY ("Id")
                );

                INSERT INTO "CourseVideos" (
                    "Id", "TenantId", "CourseId", "MediaAssetId", "Title", "SortOrder", "CreatedAtUtc"
                )
                SELECT
                    "Id", "TenantId", "CourseId", "MediaAssetId", "Title", "SortOrder", "CreatedAtUtc"
                FROM "LessonVideos"
                WHERE "LessonId" IS NULL AND "CourseId" IS NOT NULL;

                DELETE FROM "LessonVideos" WHERE "LessonId" IS NULL;

                ALTER TABLE "LessonVideos" DROP CONSTRAINT IF EXISTS "FK_LessonVideos_Courses_CourseId";
                DROP INDEX IF EXISTS "IX_LessonVideos_CourseId";
                ALTER TABLE "LessonVideos" DROP COLUMN IF EXISTS "CourseId";
                UPDATE "LessonVideos" SET "LessonId" = '00000000-0000-0000-0000-000000000000' WHERE "LessonId" IS NULL;
                ALTER TABLE "LessonVideos" ALTER COLUMN "LessonId" SET NOT NULL;
                """);
        }
    }
}
