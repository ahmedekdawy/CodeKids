using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260831220000_AddCourseVideos")]
    public class AddCourseVideos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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
                    CONSTRAINT "PK_CourseVideos" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_CourseVideos_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_CourseVideos_MediaAssets_MediaAssetId" FOREIGN KEY ("MediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE RESTRICT
                );
                CREATE INDEX IF NOT EXISTS "IX_CourseVideos_TenantId" ON "CourseVideos" ("TenantId");
                CREATE INDEX IF NOT EXISTS "IX_CourseVideos_CourseId" ON "CourseVideos" ("CourseId");
                CREATE INDEX IF NOT EXISTS "IX_CourseVideos_MediaAssetId" ON "CourseVideos" ("MediaAssetId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "CourseVideos";""");
        }
    }
}
