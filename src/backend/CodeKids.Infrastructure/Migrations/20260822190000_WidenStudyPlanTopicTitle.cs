using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260822190000_WidenStudyPlanTopicTitle")]
    public class WidenStudyPlanTopicTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "WeeklyStudyPlanTopics"
                    ALTER COLUMN "Title" TYPE character varying(1000);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "WeeklyStudyPlanTopics"
                    ALTER COLUMN "Title" TYPE character varying(300);
                """);
        }
    }
}
