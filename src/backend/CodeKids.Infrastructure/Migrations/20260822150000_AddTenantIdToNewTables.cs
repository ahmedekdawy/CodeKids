using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260822150000_AddTenantIdToNewTables")]
    public class AddTenantIdToNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE t text;
                BEGIN
                    FOREACH t IN ARRAY ARRAY[
                        'StudentWeeklyReports',
                        'WeeklyStudyPlans',
                        'WeeklyStudyPlanItems',
                        'WeeklyStudyPlanTopics',
                        'TeacherPayrollAdjustments',
                        'QuizAttemptAnswers',
                        'Stages',
                        'Grades'
                    ]
                    LOOP
                        IF EXISTS (
                            SELECT 1 FROM information_schema.tables
                            WHERE table_schema = 'public' AND table_name = t
                        ) THEN
                            EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS "TenantId" character varying(64) NULL', t);
                            EXECUTE format('CREATE INDEX IF NOT EXISTS %I ON %I ("TenantId")', 'IX_' || t || '_TenantId', t);
                        END IF;
                    END LOOP;
                END $$;

                UPDATE "StudentWeeklyReports" r
                SET "TenantId" = u."TenantId"
                FROM "Users" u
                WHERE r."TeacherId" = u."Id"
                  AND r."TenantId" IS NULL
                  AND u."TenantId" IS NOT NULL;

                UPDATE "WeeklyStudyPlans" p
                SET "TenantId" = u."TenantId"
                FROM "Users" u
                WHERE p."TeacherId" = u."Id"
                  AND p."TenantId" IS NULL
                  AND u."TenantId" IS NOT NULL;

                UPDATE "WeeklyStudyPlanItems" i
                SET "TenantId" = p."TenantId"
                FROM "WeeklyStudyPlans" p
                WHERE i."WeeklyStudyPlanId" = p."Id"
                  AND i."TenantId" IS NULL
                  AND p."TenantId" IS NOT NULL;

                UPDATE "WeeklyStudyPlanTopics" t
                SET "TenantId" = i."TenantId"
                FROM "WeeklyStudyPlanItems" i
                WHERE t."WeeklyStudyPlanItemId" = i."Id"
                  AND t."TenantId" IS NULL
                  AND i."TenantId" IS NOT NULL;

                UPDATE "TeacherPayrollAdjustments" a
                SET "TenantId" = u."TenantId"
                FROM "Users" u
                WHERE a."TeacherId" = u."Id"
                  AND a."TenantId" IS NULL
                  AND u."TenantId" IS NOT NULL;

                UPDATE "QuizAttemptAnswers" a
                SET "TenantId" = att."TenantId"
                FROM "QuizAttempts" att
                WHERE a."AttemptId" = att."Id"
                  AND a."TenantId" IS NULL
                  AND att."TenantId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS "StudentWeeklyReports" DROP COLUMN IF EXISTS "TenantId";
                ALTER TABLE IF EXISTS "WeeklyStudyPlans" DROP COLUMN IF EXISTS "TenantId";
                ALTER TABLE IF EXISTS "WeeklyStudyPlanItems" DROP COLUMN IF EXISTS "TenantId";
                ALTER TABLE IF EXISTS "WeeklyStudyPlanTopics" DROP COLUMN IF EXISTS "TenantId";
                ALTER TABLE IF EXISTS "TeacherPayrollAdjustments" DROP COLUMN IF EXISTS "TenantId";
                ALTER TABLE IF EXISTS "QuizAttemptAnswers" DROP COLUMN IF EXISTS "TenantId";
                ALTER TABLE IF EXISTS "Stages" DROP COLUMN IF EXISTS "TenantId";
                ALTER TABLE IF EXISTS "Grades" DROP COLUMN IF EXISTS "TenantId";
                """);
        }
    }
}
