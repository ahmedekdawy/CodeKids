using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOtherExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "OtherExpenses" (
                    "Id" uuid NOT NULL,
                    "Name" character varying(200) NOT NULL,
                    "Amount" numeric(18,2) NOT NULL,
                    "ExpenseDate" date NOT NULL,
                    "Notes" character varying(500) NOT NULL DEFAULT '',
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_OtherExpenses" PRIMARY KEY ("Id")
                );

                CREATE INDEX IF NOT EXISTS "IX_OtherExpenses_ExpenseDate"
                    ON "OtherExpenses" ("ExpenseDate");
                CREATE INDEX IF NOT EXISTS "IX_OtherExpenses_Name"
                    ON "OtherExpenses" ("Name");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "OtherExpenses";""");
        }
    }
}
