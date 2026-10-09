using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261009120000_AddMediaAssetFilePath")]
    public class AddMediaAssetFilePath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "MediaAssets" ADD COLUMN IF NOT EXISTS "FilePath" character varying(400) NULL;

                -- Backfill existing uploads. Terabox keys look like terabox:<fsid>|/CodeKids/2026/10/09/name.mp4,
                -- local keys are already relative. '/CodeKids' is the default Terabox:RemoteDirectory.
                UPDATE "MediaAssets"
                SET "FilePath" = regexp_replace(
                    ltrim(
                        CASE
                            WHEN "StorageKey" ILIKE 'terabox:%'
                                THEN substring("StorageKey" from position('|' in "StorageKey") + 1)
                            ELSE "StorageKey"
                        END,
                        '/'),
                    '^CodeKids/', '', 'i')
                WHERE "FilePath" IS NULL
                  AND "StorageKey" <> ''
                  AND ("StorageKey" NOT ILIKE 'terabox:%' OR position('|' in "StorageKey") > 0);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "MediaAssets" DROP COLUMN IF EXISTS "FilePath";
                """);
        }
    }
}
