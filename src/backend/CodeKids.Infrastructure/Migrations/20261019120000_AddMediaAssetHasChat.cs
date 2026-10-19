using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261019120000_AddMediaAssetHasChat")]
    public class AddMediaAssetHasChat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "MediaAssets" ADD COLUMN IF NOT EXISTS "HasChat" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE "MediaAssets" ADD COLUMN IF NOT EXISTS "YoutubeVideoKey" character varying(32) NULL;

                -- Existing YouTube links keep their live chat: the key is the same one the video URL carries.
                UPDATE "MediaAssets"
                SET "YoutubeVideoKey" = extracted.yt_key,
                    "HasChat" = TRUE
                FROM (
                    SELECT "Id",
                           substring("ExternalUrl" from '/(?:watch\?v=|embed/|live/|shorts/|v/|video/|youtu\.be/)([A-Za-z0-9_-]{6,20})') AS yt_key
                    FROM "MediaAssets"
                    WHERE "ExternalUrl" IS NOT NULL
                      AND "ExternalUrl" ~* '^https?://(www\.|m\.|music\.|studio\.)?(youtube\.com|youtu\.be)/'
                      AND "HasChat" = FALSE
                ) AS extracted
                WHERE "MediaAssets"."Id" = extracted."Id"
                  AND extracted.yt_key IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "MediaAssets" DROP COLUMN IF EXISTS "HasChat";
                ALTER TABLE "MediaAssets" DROP COLUMN IF EXISTS "YoutubeVideoKey";
                """);
        }
    }
}
