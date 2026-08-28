using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260828140000_AddChatRooms")]
    public class AddChatRooms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "ChatRooms" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64),
                    "ClassroomId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "UnitId" uuid,
                    "LessonId" uuid,
                    "Kind" character varying(20) NOT NULL,
                    "Title" character varying(400) NOT NULL,
                    "CourseTitle" character varying(300) NOT NULL DEFAULT '',
                    "UnitTitle" character varying(300) NOT NULL DEFAULT '',
                    "LessonTitle" character varying(300) NOT NULL DEFAULT '',
                    "CreatedByUserId" uuid NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_ChatRooms" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_ChatRooms_Classrooms_ClassroomId" FOREIGN KEY ("ClassroomId") REFERENCES "Classrooms" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ChatRooms_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ChatRooms_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
                );
                CREATE INDEX IF NOT EXISTS "IX_ChatRooms_TenantId" ON "ChatRooms" ("TenantId");
                CREATE INDEX IF NOT EXISTS "IX_ChatRooms_ClassroomId_CourseId_Kind" ON "ChatRooms" ("ClassroomId", "CourseId", "Kind");

                CREATE TABLE IF NOT EXISTS "ChatRoomMembers" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64),
                    "RoomId" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "IsBlocked" boolean NOT NULL DEFAULT FALSE,
                    "BlockedAtUtc" timestamp with time zone,
                    "BlockedByUserId" uuid,
                    CONSTRAINT "PK_ChatRoomMembers" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_ChatRoomMembers_ChatRooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "ChatRooms" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ChatRoomMembers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_ChatRoomMembers_RoomId_UserId" ON "ChatRoomMembers" ("RoomId", "UserId");
                CREATE INDEX IF NOT EXISTS "IX_ChatRoomMembers_TenantId" ON "ChatRoomMembers" ("TenantId");

                CREATE TABLE IF NOT EXISTS "ChatMessages" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64),
                    "RoomId" uuid NOT NULL,
                    "SenderId" uuid NOT NULL,
                    "Body" character varying(2000) NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                    "DeletedByUserId" uuid,
                    "DeletedAtUtc" timestamp with time zone,
                    CONSTRAINT "PK_ChatMessages" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_ChatMessages_ChatRooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "ChatRooms" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ChatMessages_Users_SenderId" FOREIGN KEY ("SenderId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
                );
                CREATE INDEX IF NOT EXISTS "IX_ChatMessages_TenantId" ON "ChatMessages" ("TenantId");
                CREATE INDEX IF NOT EXISTS "IX_ChatMessages_RoomId_CreatedAtUtc" ON "ChatMessages" ("RoomId", "CreatedAtUtc");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "ChatMessages";
                DROP TABLE IF EXISTS "ChatRoomMembers";
                DROP TABLE IF EXISTS "ChatRooms";
                """);
        }
    }
}
