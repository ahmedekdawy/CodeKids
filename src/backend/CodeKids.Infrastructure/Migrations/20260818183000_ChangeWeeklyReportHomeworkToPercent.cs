using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260818183000_ChangeWeeklyReportHomeworkToPercent")]
    public class ChangeWeeklyReportHomeworkToPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'StudentWeeklyReports' AND column_name = 'Homework'
                    ) THEN
                        ALTER TABLE "StudentWeeklyReports" ALTER COLUMN "Homework" DROP DEFAULT;
                        ALTER TABLE "StudentWeeklyReports" ALTER COLUMN "Homework" DROP NOT NULL;
                        ALTER TABLE "StudentWeeklyReports"
                            ALTER COLUMN "Homework" TYPE integer
                            USING CASE
                                WHEN trim("Homework") ~ '^[0-9]+$' THEN trim("Homework")::integer
                                ELSE NULL
                            END;
                        ALTER TABLE "StudentWeeklyReports" RENAME COLUMN "Homework" TO "HomeworkPercent";
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'StudentWeeklyReports' AND column_name = 'HomeworkPercent'
                    ) THEN
                        ALTER TABLE "StudentWeeklyReports" RENAME COLUMN "HomeworkPercent" TO "Homework";
                        ALTER TABLE "StudentWeeklyReports"
                            ALTER COLUMN "Homework" TYPE character varying(200)
                            USING COALESCE("Homework"::text, '');
                        ALTER TABLE "StudentWeeklyReports" ALTER COLUMN "Homework" SET NOT NULL;
                        ALTER TABLE "StudentWeeklyReports" ALTER COLUMN "Homework" SET DEFAULT '';
                    END IF;
                END $$;
                """);
        }
    }
}
