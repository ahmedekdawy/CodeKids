using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTuitionPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "TuitionPayments" (
                    "Id" uuid NOT NULL,
                    "ParentId" uuid NULL,
                    "StudentId" uuid NULL,
                    "Year" integer NOT NULL,
                    "Month" integer NOT NULL,
                    "Amount" numeric(18,2) NOT NULL,
                    "PaymentDate" date NOT NULL,
                    "Notes" character varying(500) NOT NULL DEFAULT '',
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_TuitionPayments" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_TuitionPayments_Users_ParentId'
                    ) THEN
                        ALTER TABLE "TuitionPayments"
                            ADD CONSTRAINT "FK_TuitionPayments_Users_ParentId"
                            FOREIGN KEY ("ParentId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_TuitionPayments_Users_StudentId'
                    ) THEN
                        ALTER TABLE "TuitionPayments"
                            ADD CONSTRAINT "FK_TuitionPayments_Users_StudentId"
                            FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_TuitionPayments_PaymentDate"
                    ON "TuitionPayments" ("PaymentDate");
                CREATE INDEX IF NOT EXISTS "IX_TuitionPayments_Year_Month"
                    ON "TuitionPayments" ("Year", "Month");
                CREATE INDEX IF NOT EXISTS "IX_TuitionPayments_ParentId"
                    ON "TuitionPayments" ("ParentId");
                CREATE INDEX IF NOT EXISTS "IX_TuitionPayments_StudentId"
                    ON "TuitionPayments" ("StudentId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "TuitionPayments";""");
        }
    }
}
