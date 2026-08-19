using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260819210000_StudyPlanWeekTopics")]
    public class StudyPlanWeekTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "WeeklyStudyPlanItems"
                    ADD COLUMN IF NOT EXISTS "WeekNumber" integer NOT NULL DEFAULT 1;
                ALTER TABLE "WeeklyStudyPlanItems"
                    ADD COLUMN IF NOT EXISTS "FromDate" date;
                ALTER TABLE "WeeklyStudyPlanItems"
                    ADD COLUMN IF NOT EXISTS "ToDate" date;

                UPDATE "WeeklyStudyPlanItems"
                SET "FromDate" = COALESCE("FromDate", "ItemDate"),
                    "ToDate" = COALESCE("ToDate", "ItemDate")
                WHERE "FromDate" IS NULL OR "ToDate" IS NULL;

                WITH numbered AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (PARTITION BY "WeeklyStudyPlanId" ORDER BY COALESCE("FromDate", "ItemDate"), "Id") AS n
                    FROM "WeeklyStudyPlanItems"
                )
                UPDATE "WeeklyStudyPlanItems" AS items
                SET "WeekNumber" = numbered.n
                FROM numbered
                WHERE items."Id" = numbered."Id";

                ALTER TABLE "WeeklyStudyPlanItems"
                    ALTER COLUMN "FromDate" SET NOT NULL;
                ALTER TABLE "WeeklyStudyPlanItems"
                    ALTER COLUMN "ToDate" SET NOT NULL;

                CREATE TABLE IF NOT EXISTS "WeeklyStudyPlanTopics" (
                    "Id" uuid NOT NULL,
                    "WeeklyStudyPlanItemId" uuid NOT NULL,
                    "Title" character varying(300) NOT NULL DEFAULT '',
                    "Highlight" boolean NOT NULL DEFAULT false,
                    "SortOrder" integer NOT NULL DEFAULT 0,
                    CONSTRAINT "PK_WeeklyStudyPlanTopics" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudyPlanTopics_Items_ItemId'
                    ) THEN
                        ALTER TABLE "WeeklyStudyPlanTopics"
                            ADD CONSTRAINT "FK_StudyPlanTopics_Items_ItemId"
                            FOREIGN KEY ("WeeklyStudyPlanItemId") REFERENCES "WeeklyStudyPlanItems" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;

                INSERT INTO "WeeklyStudyPlanTopics" ("Id", "WeeklyStudyPlanItemId", "Title", "Highlight", "SortOrder")
                SELECT md5(random()::text || clock_timestamp()::text || "Id"::text)::uuid,
                       "Id",
                       "Topic",
                       false,
                       0
                FROM "WeeklyStudyPlanItems"
                WHERE COALESCE(TRIM("Topic"), '') <> ''
                  AND NOT EXISTS (
                      SELECT 1 FROM "WeeklyStudyPlanTopics" t
                      WHERE t."WeeklyStudyPlanItemId" = "WeeklyStudyPlanItems"."Id"
                  );

                DROP INDEX IF EXISTS "IX_WeeklyStudyPlanItems_WeeklyStudyPlanId_ItemDate";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_WeeklyStudyPlanItems_WeeklyStudyPlanId_WeekNumber"
                    ON "WeeklyStudyPlanItems" ("WeeklyStudyPlanId", "WeekNumber");
                CREATE INDEX IF NOT EXISTS "IX_WeeklyStudyPlanTopics_WeeklyStudyPlanItemId_SortOrder"
                    ON "WeeklyStudyPlanTopics" ("WeeklyStudyPlanItemId", "SortOrder");

                ALTER TABLE "WeeklyStudyPlanItems" DROP COLUMN IF EXISTS "ItemDate";
                ALTER TABLE "WeeklyStudyPlanItems" DROP COLUMN IF EXISTS "Topic";
                ALTER TABLE "WeeklyStudyPlanItems" DROP COLUMN IF EXISTS "Classwork";
                ALTER TABLE "WeeklyStudyPlanItems" DROP COLUMN IF EXISTS "Homework";
                ALTER TABLE "WeeklyStudyPlanItems" DROP COLUMN IF EXISTS "Notes";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "WeeklyStudyPlanItems"
                    ADD COLUMN IF NOT EXISTS "ItemDate" date;
                ALTER TABLE "WeeklyStudyPlanItems"
                    ADD COLUMN IF NOT EXISTS "Topic" character varying(300) NOT NULL DEFAULT '';
                ALTER TABLE "WeeklyStudyPlanItems"
                    ADD COLUMN IF NOT EXISTS "Classwork" character varying(500) NOT NULL DEFAULT '';
                ALTER TABLE "WeeklyStudyPlanItems"
                    ADD COLUMN IF NOT EXISTS "Homework" character varying(500) NOT NULL DEFAULT '';
                ALTER TABLE "WeeklyStudyPlanItems"
                    ADD COLUMN IF NOT EXISTS "Notes" character varying(500) NOT NULL DEFAULT '';

                UPDATE "WeeklyStudyPlanItems"
                SET "ItemDate" = COALESCE("ItemDate", "FromDate");

                DROP TABLE IF EXISTS "WeeklyStudyPlanTopics";
                DROP INDEX IF EXISTS "IX_WeeklyStudyPlanItems_WeeklyStudyPlanId_WeekNumber";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_WeeklyStudyPlanItems_WeeklyStudyPlanId_ItemDate"
                    ON "WeeklyStudyPlanItems" ("WeeklyStudyPlanId", "ItemDate");

                ALTER TABLE "WeeklyStudyPlanItems" DROP COLUMN IF EXISTS "WeekNumber";
                ALTER TABLE "WeeklyStudyPlanItems" DROP COLUMN IF EXISTS "FromDate";
                ALTER TABLE "WeeklyStudyPlanItems" DROP COLUMN IF EXISTS "ToDate";
                """);
        }
    }
}
