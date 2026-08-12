using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "CourseUnits" (
                    "Id" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "Title" character varying(120) NOT NULL,
                    "Description" character varying(500) NOT NULL,
                    "SortOrder" integer NOT NULL,
                    CONSTRAINT "PK_CourseUnits" PRIMARY KEY ("Id")
                );
                CREATE INDEX IF NOT EXISTS "IX_CourseUnits_CourseId_SortOrder"
                    ON "CourseUnits" ("CourseId", "SortOrder");
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_CourseUnits_Courses_CourseId'
                    ) THEN
                        ALTER TABLE "CourseUnits"
                            ADD CONSTRAINT "FK_CourseUnits_Courses_CourseId"
                            FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;

                ALTER TABLE "Lessons" ADD COLUMN IF NOT EXISTS "UnitId" uuid NULL;
                CREATE INDEX IF NOT EXISTS "IX_Lessons_UnitId" ON "Lessons" ("UnitId");
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Lessons_CourseUnits_UnitId'
                    ) THEN
                        ALTER TABLE "Lessons"
                            ADD CONSTRAINT "FK_Lessons_CourseUnits_UnitId"
                            FOREIGN KEY ("UnitId") REFERENCES "CourseUnits" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;

                INSERT INTO "CourseUnits" ("Id", "CourseId", "Title", "Description", "SortOrder")
                SELECT gen_random_uuid(), c."Id", 'Unit 1', '', 1
                FROM "Courses" c
                WHERE EXISTS (
                    SELECT 1 FROM "Lessons" l
                    WHERE l."CourseId" = c."Id" AND l."UnitId" IS NULL
                )
                AND NOT EXISTS (
                    SELECT 1 FROM "CourseUnits" u WHERE u."CourseId" = c."Id"
                );

                UPDATE "Lessons" l
                SET "UnitId" = u."Id"
                FROM (
                    SELECT DISTINCT ON ("CourseId") "Id", "CourseId"
                    FROM "CourseUnits"
                    ORDER BY "CourseId", "SortOrder", "Title"
                ) u
                WHERE l."CourseId" = u."CourseId" AND l."UnitId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Lessons" DROP CONSTRAINT IF EXISTS "FK_Lessons_CourseUnits_UnitId";
                DROP INDEX IF EXISTS "IX_Lessons_UnitId";
                ALTER TABLE "Lessons" DROP COLUMN IF EXISTS "UnitId";
                DROP TABLE IF EXISTS "CourseUnits";
                """);
        }
    }
}
