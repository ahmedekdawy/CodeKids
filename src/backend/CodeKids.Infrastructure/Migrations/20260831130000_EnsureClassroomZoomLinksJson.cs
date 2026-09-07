using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260831130000_EnsureClassroomZoomLinksJson")]
    public class EnsureClassroomZoomLinksJson : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Classrooms"
                ADD COLUMN IF NOT EXISTS "ZoomLinksJson" text NOT NULL DEFAULT '[]';

                DO $migrate$
                BEGIN
                  IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'Classrooms'
                      AND column_name = 'ZoomMeetingLink'
                  ) THEN
                    UPDATE "Classrooms"
                    SET "ZoomLinksJson" = json_build_array(
                      json_build_object('name', 'Zoom', 'url', "ZoomMeetingLink")
                    )::text
                    WHERE COALESCE("ZoomMeetingLink", '') <> ''
                      AND COALESCE("ZoomLinksJson", '[]') = '[]';

                    ALTER TABLE "Classrooms" DROP COLUMN "ZoomMeetingLink";
                  END IF;
                END
                $migrate$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
