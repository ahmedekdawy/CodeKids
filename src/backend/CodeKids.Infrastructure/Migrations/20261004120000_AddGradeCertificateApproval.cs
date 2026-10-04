using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261004120000_AddGradeCertificateApproval")]
    public class AddGradeCertificateApproval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "GradeCertificates" ADD COLUMN IF NOT EXISTS "IsApproved" boolean NOT NULL DEFAULT false;
                ALTER TABLE "GradeCertificates" ADD COLUMN IF NOT EXISTS "ApprovedAtUtc" timestamp with time zone NULL;
                ALTER TABLE "GradeCertificates" ADD COLUMN IF NOT EXISTS "ApprovedByUserId" uuid NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "GradeCertificates" DROP COLUMN IF EXISTS "IsApproved";
                ALTER TABLE "GradeCertificates" DROP COLUMN IF EXISTS "ApprovedAtUtc";
                ALTER TABLE "GradeCertificates" DROP COLUMN IF EXISTS "ApprovedByUserId";
                """);
        }
    }
}
