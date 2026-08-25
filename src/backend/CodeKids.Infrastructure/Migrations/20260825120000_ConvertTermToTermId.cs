using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260825120000_ConvertTermToTermId")]
    public class ConvertTermToTermId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Courses'
                          AND column_name = 'TermId'
                    ) THEN
                        ALTER TABLE "Courses" ADD COLUMN "TermId" integer NULL;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Courses'
                          AND column_name = 'Term'
                    ) THEN
                        UPDATE "Courses"
                        SET "TermId" = CASE
                            WHEN "Term" IN ('FirstTerm', '1') THEN 1
                            WHEN "Term" IN ('SecondTerm', '2') THEN 2
                            WHEN "Term" IN ('FullYear', '3') THEN 3
                            ELSE NULL
                        END;
                        ALTER TABLE "Courses" DROP COLUMN "Term";
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'CourseUnits'
                          AND column_name = 'Term'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'CourseUnits'
                          AND column_name = 'TermId'
                    ) THEN
                        ALTER TABLE "CourseUnits" RENAME COLUMN "Term" TO "TermId";
                    ELSIF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'CourseUnits'
                          AND column_name = 'TermId'
                    ) THEN
                        ALTER TABLE "CourseUnits" ADD COLUMN "TermId" integer NULL;
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
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Courses'
                          AND column_name = 'Term'
                    ) THEN
                        ALTER TABLE "Courses" ADD COLUMN "Term" character varying(20) NULL;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Courses'
                          AND column_name = 'TermId'
                    ) THEN
                        UPDATE "Courses"
                        SET "Term" = CASE "TermId"
                            WHEN 1 THEN 'FirstTerm'
                            WHEN 2 THEN 'SecondTerm'
                            WHEN 3 THEN 'FullYear'
                            ELSE NULL
                        END;
                        ALTER TABLE "Courses" DROP COLUMN "TermId";
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'CourseUnits'
                          AND column_name = 'TermId'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'CourseUnits'
                          AND column_name = 'Term'
                    ) THEN
                        ALTER TABLE "CourseUnits" RENAME COLUMN "TermId" TO "Term";
                    END IF;
                END $$;
                """);
        }
    }
}
