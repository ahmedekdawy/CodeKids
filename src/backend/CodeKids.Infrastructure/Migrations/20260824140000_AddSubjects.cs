using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260824140000_AddSubjects")]
    public class AddSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "Subjects" (
                    "Id" integer NOT NULL,
                    "Title" character varying(120) NOT NULL,
                    "StageId" integer NOT NULL,
                    "TenantId" character varying(64) NULL,
                    CONSTRAINT "PK_Subjects" PRIMARY KEY ("Id")
                );

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Subjects_Stages_StageId'
                    ) THEN
                        ALTER TABLE "Subjects"
                            ADD CONSTRAINT "FK_Subjects_Stages_StageId"
                            FOREIGN KEY ("StageId") REFERENCES "Stages" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_Subjects_StageId" ON "Subjects" ("StageId");
                CREATE INDEX IF NOT EXISTS "IX_Subjects_TenantId" ON "Subjects" ("TenantId");

                INSERT INTO "Subjects" ("Id", "Title", "StageId") VALUES
                    (1, 'اللغة العربية', 2),
                    (7, 'اللغة الإنجليزية', 3),
                    (8, 'اللغة الإنجليزية', 3),
                    (10, 'Science', 2),
                    (11, 'الدراسات الاجتماعية', 2),
                    (12, 'Mathematics', 1),
                    (13, 'اللغة العربية', 2),
                    (14, 'الدراسات الاجتماعية', 2),
                    (15, 'اللغة الإنجليزية', 2),
                    (16, 'Science', 2),
                    (17, 'اللغة العربية', 2),
                    (19, 'اللغة الإنجليزية', 2),
                    (20, 'Science', 2),
                    (21, 'Mathematics', 2),
                    (22, 'اللغة العربية', 3),
                    (27, 'التاريخ', 3),
                    (37, 'العلوم', 2),
                    (38, 'العلوم', 2),
                    (39, 'العلوم', 2),
                    (40, 'اللغة العربية', 1),
                    (55, 'الرياضيات', 1),
                    (57, 'الرياضيات', 2),
                    (58, 'الرياضيات', 2),
                    (59, 'Mathematics', 2),
                    (60, 'Mathematics', 2),
                    (69, 'الرياضيات', 2),
                    (70, 'اللغة العربية', 1),
                    (71, 'اللغة الإنجليزية', 1),
                    (72, 'Science', 1),
                    (78, 'اللغة العربية', 1),
                    (79, 'اللغة الإنجليزية', 1),
                    (82, 'Science', 1),
                    (83, 'الرياضيات', 1),
                    (98, 'الرياضيات', 1),
                    (176, 'English', 1),
                    (179, 'التاريخ', 3),
                    (180, 'الجغرافيا', 3),
                    (193, 'الجغرافيا', 3),
                    (194, 'التاريخ', 3),
                    (261, 'اللغة العربية', 1),
                    (262, 'English', 1),
                    (355, 'تربية إسلامية', 1),
                    (356, 'تربية إسلامية', 1)
                ON CONFLICT ("Id") DO NOTHING;

                UPDATE "Courses"
                SET "ExternalSubjectId" = NULL
                WHERE "ExternalSubjectId" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM "Subjects" s WHERE s."Id" = "Courses"."ExternalSubjectId"
                  );

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Courses_Subjects_ExternalSubjectId'
                    ) THEN
                        ALTER TABLE "Courses"
                            ADD CONSTRAINT "FK_Courses_Subjects_ExternalSubjectId"
                            FOREIGN KEY ("ExternalSubjectId") REFERENCES "Subjects" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Courses" DROP CONSTRAINT IF EXISTS "FK_Courses_Subjects_ExternalSubjectId";
                DROP TABLE IF EXISTS "Subjects";
                """);
        }
    }
}
