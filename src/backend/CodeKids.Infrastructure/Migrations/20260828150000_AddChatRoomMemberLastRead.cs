using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260828150000_AddChatRoomMemberLastRead")]
    public class AddChatRoomMemberLastRead : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "ChatRoomMembers"
                ADD COLUMN IF NOT EXISTS "LastReadAtUtc" timestamp with time zone;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "ChatRoomMembers"
                DROP COLUMN IF EXISTS "LastReadAtUtc";
                """);
        }
    }
}
