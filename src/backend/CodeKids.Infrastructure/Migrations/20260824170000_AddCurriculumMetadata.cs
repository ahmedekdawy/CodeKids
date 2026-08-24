using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260824170000_AddCurriculumMetadata")]
    public class AddCurriculumMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Subjects" ADD COLUMN IF NOT EXISTS "Code" character varying(80) NOT NULL DEFAULT '';
                ALTER TABLE "Subjects" ADD COLUMN IF NOT EXISTS "Category" character varying(40) NOT NULL DEFAULT '';
                ALTER TABLE "Subjects" ADD COLUMN IF NOT EXISTS "NameEn" character varying(200) NOT NULL DEFAULT '';
                ALTER TABLE "Subjects" ADD COLUMN IF NOT EXISTS "Notes" character varying(1000) NOT NULL DEFAULT '';
                ALTER TABLE "Subjects" ALTER COLUMN "Title" TYPE character varying(200);
                CREATE INDEX IF NOT EXISTS "IX_Subjects_Code_StageId" ON "Subjects" ("Code", "StageId");

                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "SubjectCode" character varying(80) NOT NULL DEFAULT '';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Category" character varying(40) NOT NULL DEFAULT '';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "TrackCode" character varying(40) NOT NULL DEFAULT '';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "TrackName" character varying(80) NOT NULL DEFAULT '';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "VerificationStatus" character varying(80) NOT NULL DEFAULT '';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "SourceTocUrl" character varying(500) NOT NULL DEFAULT '';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Notes" character varying(1000) NOT NULL DEFAULT '';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Variants" character varying(400) NOT NULL DEFAULT '';
                ALTER TABLE "Courses" ALTER COLUMN "Title" TYPE character varying(200);
                ALTER TABLE "Courses" ALTER COLUMN "Description" TYPE character varying(1000);
                CREATE INDEX IF NOT EXISTS "IX_Courses_Grade_SubjectCode_TrackCode"
                    ON "Courses" ("Grade", "SubjectCode", "TrackCode");

                ALTER TABLE "CourseUnits" ADD COLUMN IF NOT EXISTS "Term" integer NULL;
                ALTER TABLE "CourseUnits" ADD COLUMN IF NOT EXISTS "VerificationStatus" character varying(80) NOT NULL DEFAULT '';
                ALTER TABLE "CourseUnits" ALTER COLUMN "Title" TYPE character varying(300);

                ALTER TABLE "Lessons" ALTER COLUMN "Title" TYPE character varying(300);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Courses_Grade_SubjectCode_TrackCode";
                DROP INDEX IF EXISTS "IX_Subjects_Code_StageId";
                ALTER TABLE "Subjects" DROP COLUMN IF EXISTS "Code";
                ALTER TABLE "Subjects" DROP COLUMN IF EXISTS "Category";
                ALTER TABLE "Subjects" DROP COLUMN IF EXISTS "NameEn";
                ALTER TABLE "Subjects" DROP COLUMN IF EXISTS "Notes";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "SubjectCode";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "Category";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "TrackCode";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "TrackName";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "VerificationStatus";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "SourceTocUrl";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "Notes";
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "Variants";
                ALTER TABLE "CourseUnits" DROP COLUMN IF EXISTS "Term";
                ALTER TABLE "CourseUnits" DROP COLUMN IF EXISTS "VerificationStatus";
                """);
        }
    }
}
