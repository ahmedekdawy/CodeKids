using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAssetExternalUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "MediaAssets"
                    ADD COLUMN IF NOT EXISTS "ExternalUrl" character varying(1000) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "MediaAssets" DROP COLUMN IF EXISTS "ExternalUrl";
                """);
        }
    }
}
