using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <summary>
    /// Removes the student- and teacher-only columns left on "Users" by AddUserRoleProfiles.
    /// Their data lives in "StudentProfiles" and "TeacherProfiles".
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261011120000_DropLegacyUserRoleColumns")]
    public class DropLegacyUserRoleColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                    DROP COLUMN IF EXISTS "ParentId",
                    DROP COLUMN IF EXISTS "AvatarId",
                    DROP COLUMN IF EXISTS "Grade",
                    DROP COLUMN IF EXISTS "SchoolType",
                    DROP COLUMN IF EXISTS "TotalXp",
                    DROP COLUMN IF EXISTS "WorkShift",
                    DROP COLUMN IF EXISTS "Stages",
                    DROP COLUMN IF EXISTS "ContractType",
                    DROP COLUMN IF EXISTS "PrimaryAmount",
                    DROP COLUMN IF EXISTS "PrepAmount",
                    DROP COLUMN IF EXISTS "SecondaryAmount",
                    DROP COLUMN IF EXISTS "MonthlySalary",
                    DROP COLUMN IF EXISTS "ZoomAccessToken",
                    DROP COLUMN IF EXISTS "ZoomRefreshToken",
                    DROP COLUMN IF EXISTS "ZoomTokenExpiresAt",
                    DROP COLUMN IF EXISTS "ZoomConnectedEmail";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreates the columns and refills them from the profile tables.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                    ADD COLUMN IF NOT EXISTS "ParentId" uuid NULL,
                    ADD COLUMN IF NOT EXISTS "AvatarId" uuid NULL,
                    ADD COLUMN IF NOT EXISTS "Grade" integer NULL,
                    ADD COLUMN IF NOT EXISTS "SchoolType" character varying(20) NULL,
                    ADD COLUMN IF NOT EXISTS "TotalXp" integer NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "WorkShift" character varying(20) NULL,
                    ADD COLUMN IF NOT EXISTS "Stages" character varying(40) NOT NULL DEFAULT '',
                    ADD COLUMN IF NOT EXISTS "ContractType" character varying(20) NULL,
                    ADD COLUMN IF NOT EXISTS "PrimaryAmount" numeric(18,2) NULL,
                    ADD COLUMN IF NOT EXISTS "PrepAmount" numeric(18,2) NULL,
                    ADD COLUMN IF NOT EXISTS "SecondaryAmount" numeric(18,2) NULL,
                    ADD COLUMN IF NOT EXISTS "MonthlySalary" numeric(18,2) NULL,
                    ADD COLUMN IF NOT EXISTS "ZoomAccessToken" character varying(2000) NOT NULL DEFAULT '',
                    ADD COLUMN IF NOT EXISTS "ZoomRefreshToken" character varying(2000) NOT NULL DEFAULT '',
                    ADD COLUMN IF NOT EXISTS "ZoomTokenExpiresAt" timestamp with time zone NULL,
                    ADD COLUMN IF NOT EXISTS "ZoomConnectedEmail" character varying(160) NOT NULL DEFAULT '';

                UPDATE "Users" u
                SET "ParentId" = s."ParentId", "AvatarId" = s."AvatarId", "Grade" = s."Grade",
                    "SchoolType" = s."SchoolType", "TotalXp" = s."TotalXp"
                FROM "StudentProfiles" s
                WHERE s."UserId" = u."Id";

                UPDATE "Users" u
                SET "WorkShift" = t."WorkShift", "Stages" = t."Stages", "ContractType" = t."ContractType",
                    "PrimaryAmount" = t."PrimaryAmount", "PrepAmount" = t."PrepAmount",
                    "SecondaryAmount" = t."SecondaryAmount", "MonthlySalary" = t."MonthlySalary",
                    "ZoomAccessToken" = t."ZoomAccessToken", "ZoomRefreshToken" = t."ZoomRefreshToken",
                    "ZoomTokenExpiresAt" = t."ZoomTokenExpiresAt", "ZoomConnectedEmail" = t."ZoomConnectedEmail"
                FROM "TeacherProfiles" t
                WHERE t."UserId" = u."Id";
                """);
        }
    }
}
