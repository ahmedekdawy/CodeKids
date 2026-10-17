using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261017120000_AddRolesAndPages")]
    public class AddRolesAndPages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "Pages" (
                    "Id" uuid NOT NULL,
                    "Key" character varying(160) NOT NULL,
                    "LabelKey" character varying(120) NOT NULL,
                    "Icon" character varying(16) NOT NULL,
                    "CategoryKey" character varying(80) NOT NULL,
                    "Panel" character varying(20) NOT NULL,
                    "SortOrder" integer NOT NULL,
                    "TenantId" character varying(64) NULL,
                    CONSTRAINT "PK_Pages" PRIMARY KEY ("Id")
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Pages_TenantId_Key" ON "Pages" ("TenantId", "Key");
                CREATE INDEX IF NOT EXISTS "IX_Pages_TenantId" ON "Pages" ("TenantId");

                CREATE TABLE IF NOT EXISTS "Roles" (
                    "Id" uuid NOT NULL,
                    "Name" character varying(80) NOT NULL,
                    "DisplayName" character varying(120) NOT NULL,
                    "Description" character varying(300) NOT NULL,
                    "IsSystem" boolean NOT NULL,
                    "BaseRole" character varying(30) NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "TenantId" character varying(64) NULL,
                    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Roles_TenantId_Name" ON "Roles" ("TenantId", "Name");
                CREATE INDEX IF NOT EXISTS "IX_Roles_TenantId" ON "Roles" ("TenantId");

                CREATE TABLE IF NOT EXISTS "RolePages" (
                    "RoleId" uuid NOT NULL,
                    "PageId" uuid NOT NULL,
                    "SortOrder" integer NOT NULL,
                    "TenantId" character varying(64) NULL,
                    CONSTRAINT "PK_RolePages" PRIMARY KEY ("RoleId", "PageId"),
                    CONSTRAINT "FK_RolePages_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_RolePages_Pages_PageId" FOREIGN KEY ("PageId") REFERENCES "Pages" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_RolePages_PageId" ON "RolePages" ("PageId");
                CREATE INDEX IF NOT EXISTS "IX_RolePages_TenantId" ON "RolePages" ("TenantId");

                CREATE TABLE IF NOT EXISTS "UserInRoles" (
                    "Id" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "RoleId" uuid NOT NULL,
                    "AssignedAtUtc" timestamp with time zone NOT NULL,
                    "TenantId" character varying(64) NULL,
                    CONSTRAINT "PK_UserInRoles" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_UserInRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_UserInRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserInRoles_TenantId_UserId_RoleId" ON "UserInRoles" ("TenantId", "UserId", "RoleId");
                CREATE INDEX IF NOT EXISTS "IX_UserInRoles_RoleId" ON "UserInRoles" ("RoleId");
                CREATE INDEX IF NOT EXISTS "IX_UserInRoles_TenantId" ON "UserInRoles" ("TenantId");

                CREATE TABLE IF NOT EXISTS "UserMenuPreferences" (
                    "Id" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "PageId" uuid NOT NULL,
                    "SortOrder" integer NOT NULL,
                    "IsHidden" boolean NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone NOT NULL,
                    "TenantId" character varying(64) NULL,
                    CONSTRAINT "PK_UserMenuPreferences" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_UserMenuPreferences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_UserMenuPreferences_Pages_PageId" FOREIGN KEY ("PageId") REFERENCES "Pages" ("Id") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserMenuPreferences_TenantId_UserId_PageId" ON "UserMenuPreferences" ("TenantId", "UserId", "PageId");
                CREATE INDEX IF NOT EXISTS "IX_UserMenuPreferences_UserId" ON "UserMenuPreferences" ("UserId");
                CREATE INDEX IF NOT EXISTS "IX_UserMenuPreferences_TenantId" ON "UserMenuPreferences" ("TenantId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "UserMenuPreferences";
                DROP TABLE IF EXISTS "UserInRoles";
                DROP TABLE IF EXISTS "RolePages";
                DROP TABLE IF EXISTS "Roles";
                DROP TABLE IF EXISTS "Pages";
                """);
        }
    }
}
