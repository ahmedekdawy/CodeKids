using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260915220000_AddLearningMaterials")]
    public class AddLearningMaterials : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "LearningMaterials" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64) NULL,
                    "CourseId" uuid NOT NULL,
                    "UnitId" uuid NULL,
                    "LessonId" uuid NULL,
                    "MediaAssetId" uuid NOT NULL,
                    "Title" character varying(200) NOT NULL,
                    "Kind" character varying(20) NOT NULL,
                    "SortOrder" integer NOT NULL,
                    "UploadedByUserId" uuid NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_LearningMaterials" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_LearningMaterials_Courses_CourseId"
                        FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_LearningMaterials_MediaAssets_MediaAssetId"
                        FOREIGN KEY ("MediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_LearningMaterials_Users_UploadedByUserId"
                        FOREIGN KEY ("UploadedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS "IX_LearningMaterials_TenantId" ON "LearningMaterials" ("TenantId");
                CREATE INDEX IF NOT EXISTS "IX_LearningMaterials_CourseId" ON "LearningMaterials" ("CourseId");
                CREATE INDEX IF NOT EXISTS "IX_LearningMaterials_UnitId" ON "LearningMaterials" ("UnitId");
                CREATE INDEX IF NOT EXISTS "IX_LearningMaterials_LessonId" ON "LearningMaterials" ("LessonId");
                CREATE INDEX IF NOT EXISTS "IX_LearningMaterials_MediaAssetId" ON "LearningMaterials" ("MediaAssetId");
                CREATE INDEX IF NOT EXISTS "IX_LearningMaterials_UploadedByUserId" ON "LearningMaterials" ("UploadedByUserId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "LearningMaterials";
                """);
        }
    }
}
