using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260824150000_UpdateCourseExternalSubjectIdsFromSubjects")]
    public class UpdateCourseExternalSubjectIdsFromSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Courses" SET "ExternalSubjectId" = NULL;

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
                ),
                picked AS (
                    SELECT DISTINCT ON (m.id) c."Id" AS course_id, m.id
                    FROM mapping m
                    INNER JOIN "Subjects" s ON s."Id" = m.id
                    INNER JOIN "Courses" c
                        ON c."Grade" = m.grade
                       AND (
                            c."Title" = m.title
                            OR (c."Title" = 'التربية الدينية' AND m.title = 'تربية إسلامية')
                       )
                    ORDER BY m.id, c."Id"
                )
                UPDATE "Courses" c
                SET "ExternalSubjectId" = p.id
                FROM picked p
                WHERE c."Id" = p.course_id
                  AND NOT EXISTS (
                      SELECT 1 FROM "Courses" other
                      WHERE other."ExternalSubjectId" = p.id AND other."Id" <> p.course_id
                  );

                WITH leftover AS (
                    SELECT
                        c."Id" AS course_id,
                        s."Id" AS subject_id,
                        ROW_NUMBER() OVER (PARTITION BY c."Id" ORDER BY s."Id") AS course_rn,
                        ROW_NUMBER() OVER (PARTITION BY s."Id" ORDER BY c."Id") AS subject_rn
                    FROM "Courses" c
                    INNER JOIN "Subjects" s
                        ON s."StageId" = c."StageId"
                       AND (
                            s."Title" = c."Title"
                            OR (c."Title" = 'التربية الدينية' AND s."Title" = 'تربية إسلامية')
                       )
                    WHERE c."ExternalSubjectId" IS NULL
                      AND NOT EXISTS (
                          SELECT 1 FROM "Courses" taken
                          WHERE taken."ExternalSubjectId" = s."Id"
                      )
                )
                UPDATE "Courses" c
                SET "ExternalSubjectId" = leftover.subject_id
                FROM leftover
                WHERE c."Id" = leftover.course_id
                  AND leftover.course_rn = 1
                  AND leftover.subject_rn = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
