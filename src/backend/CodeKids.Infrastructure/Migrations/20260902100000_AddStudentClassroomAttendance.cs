using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260902100000_AddStudentClassroomAttendance")]
    public class AddStudentClassroomAttendance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "StudentClassroomAttendances" (
                    "Id" uuid NOT NULL,
                    "StudentId" uuid NOT NULL,
                    "ClassroomId" uuid NOT NULL,
                    "AttendanceDate" date NOT NULL,
                    "Status" character varying(20) NOT NULL,
                    "RecordedByTeacherId" uuid NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "TenantId" character varying(64),
                    CONSTRAINT "PK_StudentClassroomAttendances" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentClassroomAttendances_Users_StudentId'
                    ) THEN
                        ALTER TABLE "StudentClassroomAttendances"
                            ADD CONSTRAINT "FK_StudentClassroomAttendances_Users_StudentId"
                            FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentClassroomAttendances_Classrooms_ClassroomId'
                    ) THEN
                        ALTER TABLE "StudentClassroomAttendances"
                            ADD CONSTRAINT "FK_StudentClassroomAttendances_Classrooms_ClassroomId"
                            FOREIGN KEY ("ClassroomId") REFERENCES "Classrooms" ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentClassroomAttendances_Users_RecordedByTeacherId'
                    ) THEN
                        ALTER TABLE "StudentClassroomAttendances"
                            ADD CONSTRAINT "FK_StudentClassroomAttendances_Users_RecordedByTeacherId"
                            FOREIGN KEY ("RecordedByTeacherId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_StudentClassroomAttendances_StudentId_ClassroomId_AttendanceDate"
                    ON "StudentClassroomAttendances" ("StudentId", "ClassroomId", "AttendanceDate");
                CREATE INDEX IF NOT EXISTS "IX_StudentClassroomAttendances_AttendanceDate"
                    ON "StudentClassroomAttendances" ("AttendanceDate");
                CREATE INDEX IF NOT EXISTS "IX_StudentClassroomAttendances_ClassroomId"
                    ON "StudentClassroomAttendances" ("ClassroomId");
                CREATE INDEX IF NOT EXISTS "IX_StudentClassroomAttendances_TenantId"
                    ON "StudentClassroomAttendances" ("TenantId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "StudentClassroomAttendances";
                """);
        }
    }
}
