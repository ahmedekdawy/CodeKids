using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261011140000_AddAvailabilityAndVacations")]
    public class AddAvailabilityAndVacations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "UserAvailabilities" (
                    "Id" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "DayOfWeek" integer NOT NULL,
                    "StartMinutes" integer NOT NULL,
                    "EndMinutes" integer NOT NULL,
                    "TenantId" character varying(64) NULL,
                    CONSTRAINT "PK_UserAvailabilities" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_UserAvailabilities_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_UserAvailabilities_UserId_DayOfWeek" ON "UserAvailabilities" ("UserId", "DayOfWeek");
                CREATE INDEX IF NOT EXISTS "IX_UserAvailabilities_TenantId" ON "UserAvailabilities" ("TenantId");

                CREATE TABLE IF NOT EXISTS "TeacherVacations" (
                    "Id" uuid NOT NULL,
                    "TeacherId" uuid NOT NULL,
                    "StartDate" date NOT NULL,
                    "EndDate" date NOT NULL,
                    "Note" character varying(200) NOT NULL DEFAULT '',
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "TenantId" character varying(64) NULL,
                    CONSTRAINT "PK_TeacherVacations" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_TeacherVacations_Users_TeacherId" FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_TeacherVacations_TeacherId_StartDate" ON "TeacherVacations" ("TeacherId", "StartDate");
                CREATE INDEX IF NOT EXISTS "IX_TeacherVacations_TenantId" ON "TeacherVacations" ("TenantId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "TeacherVacations";
                DROP TABLE IF EXISTS "UserAvailabilities";
                """);
        }
    }
}
