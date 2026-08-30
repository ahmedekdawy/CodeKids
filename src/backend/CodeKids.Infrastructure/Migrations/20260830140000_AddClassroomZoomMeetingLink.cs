using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260830140000_AddClassroomZoomMeetingLink")]
    public class AddClassroomZoomMeetingLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Classrooms"
                ADD COLUMN IF NOT EXISTS "ZoomMeetingLink" character varying(500) NOT NULL DEFAULT '';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Classrooms" DROP COLUMN IF EXISTS "ZoomMeetingLink";
                """);
        }
    }
}
