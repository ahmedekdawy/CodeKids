using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <summary>
    /// Moves the student- and teacher-only columns of "Users" into "StudentProfiles" and "TeacherProfiles",
    /// and adds "ParentProfiles". The old "Users" columns are left in place (unused, with defaults) so the
    /// previous API version keeps working until the new one is deployed; drop them in a later migration.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261010130000_AddUserRoleProfiles")]
    public class AddUserRoleProfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "StudentProfiles" (
                    "UserId" uuid NOT NULL,
                    "ParentId" uuid NULL,
                    "AvatarId" uuid NULL,
                    "Grade" integer NULL,
                    "SchoolType" character varying(20) NULL,
                    "TotalXp" integer NOT NULL DEFAULT 0,
                    CONSTRAINT "PK_StudentProfiles" PRIMARY KEY ("UserId"),
                    CONSTRAINT "FK_StudentProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_StudentProfiles_Users_ParentId" FOREIGN KEY ("ParentId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_StudentProfiles_Avatars_AvatarId" FOREIGN KEY ("AvatarId") REFERENCES "Avatars" ("Id") ON DELETE SET NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_StudentProfiles_ParentId" ON "StudentProfiles" ("ParentId");
                CREATE INDEX IF NOT EXISTS "IX_StudentProfiles_AvatarId" ON "StudentProfiles" ("AvatarId");

                CREATE TABLE IF NOT EXISTS "TeacherProfiles" (
                    "UserId" uuid NOT NULL,
                    "WorkShift" character varying(20) NULL,
                    "Stages" character varying(40) NOT NULL DEFAULT '',
                    "ContractType" character varying(20) NULL,
                    "PrimaryAmount" numeric(18,2) NULL,
                    "PrepAmount" numeric(18,2) NULL,
                    "SecondaryAmount" numeric(18,2) NULL,
                    "MonthlySalary" numeric(18,2) NULL,
                    "ZoomAccessToken" character varying(2000) NOT NULL DEFAULT '',
                    "ZoomRefreshToken" character varying(2000) NOT NULL DEFAULT '',
                    "ZoomTokenExpiresAt" timestamp with time zone NULL,
                    "ZoomConnectedEmail" character varying(160) NOT NULL DEFAULT '',
                    CONSTRAINT "PK_TeacherProfiles" PRIMARY KEY ("UserId"),
                    CONSTRAINT "FK_TeacherProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS "ParentProfiles" (
                    "UserId" uuid NOT NULL,
                    CONSTRAINT "PK_ParentProfiles" PRIMARY KEY ("UserId"),
                    CONSTRAINT "FK_ParentProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );

                -- Copy existing data. Every student/teacher/parent gets a row; so does anyone of another
                -- role who already has data in these columns (e.g. an admin who connected Zoom).
                INSERT INTO "StudentProfiles" ("UserId", "ParentId", "AvatarId", "Grade", "SchoolType", "TotalXp")
                SELECT "Id", "ParentId", "AvatarId", "Grade", "SchoolType", "TotalXp"
                FROM "Users"
                WHERE "Role" = 'Student'
                   OR "ParentId" IS NOT NULL
                   OR "AvatarId" IS NOT NULL
                   OR "Grade" IS NOT NULL
                   OR "SchoolType" IS NOT NULL
                   OR "TotalXp" <> 0
                ON CONFLICT ("UserId") DO NOTHING;

                INSERT INTO "TeacherProfiles" (
                    "UserId", "WorkShift", "Stages", "ContractType", "PrimaryAmount", "PrepAmount", "SecondaryAmount",
                    "MonthlySalary", "ZoomAccessToken", "ZoomRefreshToken", "ZoomTokenExpiresAt", "ZoomConnectedEmail")
                SELECT "Id", "WorkShift", "Stages", "ContractType", "PrimaryAmount", "PrepAmount", "SecondaryAmount",
                       "MonthlySalary", "ZoomAccessToken", "ZoomRefreshToken", "ZoomTokenExpiresAt", "ZoomConnectedEmail"
                FROM "Users"
                WHERE "Role" = 'Teacher'
                   OR "WorkShift" IS NOT NULL
                   OR "Stages" <> ''
                   OR "ContractType" IS NOT NULL
                   OR "PrimaryAmount" IS NOT NULL
                   OR "PrepAmount" IS NOT NULL
                   OR "SecondaryAmount" IS NOT NULL
                   OR "MonthlySalary" IS NOT NULL
                   OR "ZoomAccessToken" <> ''
                   OR "ZoomRefreshToken" <> ''
                   OR "ZoomConnectedEmail" <> ''
                ON CONFLICT ("UserId") DO NOTHING;

                INSERT INTO "ParentProfiles" ("UserId")
                SELECT "Id" FROM "Users" WHERE "Role" = 'Parent'
                ON CONFLICT ("UserId") DO NOTHING;

                -- The old columns stay for now but nothing writes them any more: give the NOT NULL ones
                -- defaults so inserts succeed, and drop their foreign keys so they cannot block deletes.
                ALTER TABLE "Users" ALTER COLUMN "TotalXp" SET DEFAULT 0;
                ALTER TABLE "Users" ALTER COLUMN "Stages" SET DEFAULT '';
                ALTER TABLE "Users" ALTER COLUMN "ZoomAccessToken" SET DEFAULT '';
                ALTER TABLE "Users" ALTER COLUMN "ZoomRefreshToken" SET DEFAULT '';
                ALTER TABLE "Users" ALTER COLUMN "ZoomConnectedEmail" SET DEFAULT '';
                ALTER TABLE "Users" DROP CONSTRAINT IF EXISTS "FK_Users_Users_ParentId";
                ALTER TABLE "Users" DROP CONSTRAINT IF EXISTS "FK_Users_Avatars_AvatarId";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "ParentProfiles";
                DROP TABLE IF EXISTS "TeacherProfiles";
                DROP TABLE IF EXISTS "StudentProfiles";
                ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Users_ParentId" FOREIGN KEY ("ParentId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Avatars_AvatarId" FOREIGN KEY ("AvatarId") REFERENCES "Avatars" ("Id") ON DELETE SET NULL;
                """);
        }
    }
}
