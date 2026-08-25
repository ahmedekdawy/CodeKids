using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260825120000_AddUserLastLoginDate")]
    public class AddUserLastLoginDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                    ADD COLUMN IF NOT EXISTS "LastLoginDateUtc" timestamp with time zone NULL;

                CREATE INDEX IF NOT EXISTS "IX_Users_LastLoginDateUtc"
                    ON "Users" ("LastLoginDateUtc");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Users_LastLoginDateUtc";
                ALTER TABLE "Users" DROP COLUMN IF EXISTS "LastLoginDateUtc";
                """);
        }
    }
}
