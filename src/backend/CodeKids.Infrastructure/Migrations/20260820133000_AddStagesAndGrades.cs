using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260820133000_AddStagesAndGrades")]
    public class AddStagesAndGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "Stages" (
                    "Id" integer NOT NULL,
                    "Name" character varying(80) NOT NULL,
                    "NameEn" character varying(80) NOT NULL,
                    CONSTRAINT "PK_Stages" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "Grades" (
                    "Id" integer NOT NULL,
                    "Name" character varying(80) NOT NULL,
                    "NameEn" character varying(80) NOT NULL,
                    "StageId" integer NOT NULL,
                    CONSTRAINT "PK_Grades" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Grades_Stages_StageId'
                    ) THEN
                        ALTER TABLE "Grades"
                            ADD CONSTRAINT "FK_Grades_Stages_StageId"
                            FOREIGN KEY ("StageId") REFERENCES "Stages" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_Grades_StageId" ON "Grades" ("StageId");

                INSERT INTO "Stages" ("Id", "Name", "NameEn") VALUES
                    (0, 'رياض الأطفال', 'Kindergarten'),
                    (1, 'المرحلة الابتدائية', 'Primary'),
                    (2, 'المرحلة الإعدادية', 'Preparatory'),
                    (3, 'المرحلة الثانوية', 'Secondary')
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "Grades" ("Id", "Name", "NameEn", "StageId") VALUES
                    (-1, 'KG1', 'KG1', 0),
                    (0, 'KG2', 'KG2', 0),
                    (1, 'الصف 1', 'Grade 1', 1),
                    (2, 'الصف 2', 'Grade 2', 1),
                    (3, 'الصف 3', 'Grade 3', 1),
                    (4, 'الصف 4', 'Grade 4', 1),
                    (5, 'الصف 5', 'Grade 5', 1),
                    (6, 'الصف 6', 'Grade 6', 1),
                    (7, 'الصف 7', 'Grade 7', 2),
                    (8, 'الصف 8', 'Grade 8', 2),
                    (9, 'الصف 9', 'Grade 9', 2),
                    (10, 'الصف 10', 'Grade 10', 3),
                    (11, 'الصف 11', 'Grade 11', 3),
                    (12, 'الصف 12', 'Grade 12', 3)
                ON CONFLICT ("Id") DO NOTHING;

                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "StageId" integer NULL;

                UPDATE "Courses"
                SET "StageId" = CASE
                    WHEN "Grade" IN (-1, 0) THEN 0
                    WHEN "Grade" BETWEEN 1 AND 6 THEN 1
                    WHEN "Grade" BETWEEN 7 AND 9 THEN 2
                    WHEN "Grade" BETWEEN 10 AND 12 THEN 3
                    ELSE "StageId"
                END
                WHERE "Grade" IS NOT NULL AND "StageId" IS NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Courses_Stages_StageId'
                    ) THEN
                        ALTER TABLE "Courses"
                            ADD CONSTRAINT "FK_Courses_Stages_StageId"
                            FOREIGN KEY ("StageId") REFERENCES "Stages" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_Courses_StageId" ON "Courses" ("StageId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Courses" DROP CONSTRAINT IF EXISTS "FK_Courses_Stages_StageId";
                DROP INDEX IF EXISTS "IX_Courses_StageId";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "StageId";
                DROP TABLE IF EXISTS "Grades";
                DROP TABLE IF EXISTS "Stages";
                """);
        }
    }
}
