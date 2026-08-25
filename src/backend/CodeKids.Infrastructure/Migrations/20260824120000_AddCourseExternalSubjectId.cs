using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260824120000_AddCourseExternalSubjectId")]
    public class AddCourseExternalSubjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "ExternalSubjectId" integer NULL;

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
                    INNER JOIN "Courses" c ON c."Title" = m.title AND c."Grade" = m.grade
                    ORDER BY m.id, c."Title"
                )
                UPDATE "Courses" c
                SET "ExternalSubjectId" = p.id
                FROM picked p
                WHERE c."Id" = p.course_id;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Courses_ExternalSubjectId"
                    ON "Courses" ("ExternalSubjectId")
                    WHERE "ExternalSubjectId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Courses_ExternalSubjectId";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "ExternalSubjectId";
                """);
        }
    }
}
