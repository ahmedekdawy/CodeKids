using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260904090000_AddUserProfilePhoto")]
    public class AddUserProfilePhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                    ADD COLUMN IF NOT EXISTS "ProfilePhotoStorageKey" character varying(400) NULL;

                ALTER TABLE "Users"
                    ADD COLUMN IF NOT EXISTS "ProfilePhotoContentType" character varying(120) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users" DROP COLUMN IF EXISTS "ProfilePhotoContentType";
                ALTER TABLE "Users" DROP COLUMN IF EXISTS "ProfilePhotoStorageKey";
                """);
        }
    }
}
