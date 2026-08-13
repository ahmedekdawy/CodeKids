using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseSchoolType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "SchoolType" character varying(20) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Courses" DROP COLUMN IF EXISTS "SchoolType";
                """);
        }
    }
}
