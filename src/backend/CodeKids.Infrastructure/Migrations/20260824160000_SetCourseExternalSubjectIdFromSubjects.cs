using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260824160000_SetCourseExternalSubjectIdFromSubjects")]
    public class SetCourseExternalSubjectIdFromSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Courses_ExternalSubjectId";
                CREATE INDEX IF NOT EXISTS "IX_Courses_ExternalSubjectId" ON "Courses" ("ExternalSubjectId");

                WITH mapping(title, grade, id) AS (
                    VALUES
                        ('التاريخ', 11, 179),
                        ('اللغة العربية', 8, 17),
                        ('Science', 6, 82),
                        ('اللغة الإنجليزية', 12, 8),
                        ('Science', 8, 20),
                        ('English', 2, 176),
                        ('English', 3, 262),
                        ('Mathematics', 5, 12),
                        ('الجغرافيا', 11, 180),
                        ('التاريخ', 10, 27),
                        ('اللغة العربية', 6, 78),
                        ('اللغة العربية', 9, 1),
                        ('الرياضيات', 3, 98),
                        ('اللغة الإنجليزية', 6, 79),
                        ('اللغة العربية', 7, 13),
                        ('الرياضيات', 9, 69),
                        ('Science', 9, 10),
                        ('التاريخ', 12, 194),
                        ('الدراسات الاجتماعية', 9, 11),
                        ('الجغرافيا', 12, 193),
                        ('اللغة الإنجليزية', 11, 7),
                        ('اللغة العربية', 10, 22),
                        ('اللغة الإنجليزية', 8, 19),
                        ('اللغة العربية', 3, 261),
                        ('الرياضيات', 8, 58),
                        ('Mathematics', 8, 60),
                        ('تربية إسلامية', 4, 356),
                        ('التربية الدينية', 4, 356),
                        ('العلوم', 8, 38),
                        ('Mathematics', 9, 21),
                        ('تربية إسلامية', 5, 355),
                        ('التربية الدينية', 5, 355),
                        ('العلوم', 9, 39),
                        ('اللغة العربية', 5, 70),
                        ('اللغة الإنجليزية', 7, 15),
                        ('اللغة الإنجليزية', 5, 71),
                        ('الرياضيات', 5, 55),
                        ('الرياضيات', 7, 57),
                        ('Mathematics', 7, 59),
                        ('العلوم', 7, 37),
                        ('Science', 7, 16),
                        ('الدراسات الاجتماعية', 7, 14),
                        ('اللغة العربية', 4, 40),
                        ('الرياضيات', 4, 83),
                        ('Science', 5, 72)
                )
                UPDATE "Courses" c
                SET "ExternalSubjectId" = COALESCE(
                    (
                        SELECT m.id
                        FROM mapping m
                        INNER JOIN "Subjects" s ON s."Id" = m.id
                        WHERE c."Grade" = m.grade
                          AND (
                              c."Title" = m.title
                              OR (c."Title" = 'التربية الدينية' AND m.title = 'تربية إسلامية')
                          )
                        LIMIT 1
                    ),
                    (
                        SELECT s."Id"
                        FROM "Subjects" s
                        WHERE (
                                  s."Title" = c."Title"
                                  OR (c."Title" = 'التربية الدينية' AND s."Title" = 'تربية إسلامية')
                              )
                          AND s."StageId" = COALESCE(
                              c."StageId",
                              CASE
                                  WHEN c."Grade" IS NULL THEN NULL
                                  WHEN c."Grade" <= 0 THEN 0
                                  WHEN c."Grade" <= 6 THEN 1
                                  WHEN c."Grade" <= 9 THEN 2
                                  ELSE 3
                              END
                          )
                        ORDER BY s."Id"
                        LIMIT 1
                    )
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM "Subjects" s
                    WHERE s."Id" IN (
                        SELECT m.id FROM mapping m
                        WHERE c."Grade" = m.grade
                          AND (
                              c."Title" = m.title
                              OR (c."Title" = 'التربية الدينية' AND m.title = 'تربية إسلامية')
                          )
                    )
                    OR (
                        (
                            s."Title" = c."Title"
                            OR (c."Title" = 'التربية الدينية' AND s."Title" = 'تربية إسلامية')
                        )
                        AND s."StageId" = COALESCE(
                            c."StageId",
                            CASE
                                WHEN c."Grade" IS NULL THEN NULL
                                WHEN c."Grade" <= 0 THEN 0
                                WHEN c."Grade" <= 6 THEN 1
                                WHEN c."Grade" <= 9 THEN 2
                                ELSE 3
                            END
                        )
                    )
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Courses_ExternalSubjectId";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Courses_ExternalSubjectId"
                    ON "Courses" ("ExternalSubjectId")
                    WHERE "ExternalSubjectId" IS NOT NULL;
                """);
        }
    }
}
