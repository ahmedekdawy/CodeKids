using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260818210000_AddTeacherPayrollAdjustments")]
    public class AddTeacherPayrollAdjustments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                    ADD COLUMN IF NOT EXISTS "MonthlySalary" numeric(18,2) NULL;

                CREATE TABLE IF NOT EXISTS "TeacherPayrollAdjustments" (
                    "Id" uuid NOT NULL,
                    "TeacherId" uuid NOT NULL,
                    "Amount" numeric(18,2) NOT NULL,
                    "AdjustmentDate" date NOT NULL,
                    "Notes" character varying(500) NOT NULL DEFAULT '',
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_TeacherPayrollAdjustments" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_TeacherPayrollAdjustments_Users_TeacherId'
                    ) THEN
                        ALTER TABLE "TeacherPayrollAdjustments"
                            ADD CONSTRAINT "FK_TeacherPayrollAdjustments_Users_TeacherId"
                            FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_TeacherPayrollAdjustments_AdjustmentDate"
                    ON "TeacherPayrollAdjustments" ("AdjustmentDate");
                CREATE INDEX IF NOT EXISTS "IX_TeacherPayrollAdjustments_TeacherId"
                    ON "TeacherPayrollAdjustments" ("TeacherId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "TeacherPayrollAdjustments";
                ALTER TABLE "Users" DROP COLUMN IF EXISTS "MonthlySalary";
                """);
        }
    }
}
