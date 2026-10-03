using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261003120000_AddGradeCertificates")]
    public class AddGradeCertificates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "GradeCertificates" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64) NULL,
                    "ClassroomId" uuid NOT NULL,
                    "Title" character varying(200) NOT NULL,
                    "CreatedByUserId" uuid NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_GradeCertificates" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_GradeCertificates_Classrooms_ClassroomId"
                        FOREIGN KEY ("ClassroomId") REFERENCES "Classrooms" ("Id") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS "IX_GradeCertificates_TenantId" ON "GradeCertificates" ("TenantId");
                CREATE INDEX IF NOT EXISTS "IX_GradeCertificates_ClassroomId" ON "GradeCertificates" ("ClassroomId");

                CREATE TABLE IF NOT EXISTS "GradeCertificateSubjects" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64) NULL,
                    "CertificateId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "MaxDegree" numeric(7,2) NOT NULL,
                    "IncludedInTotal" boolean NOT NULL,
                    "SortOrder" integer NOT NULL,
                    CONSTRAINT "PK_GradeCertificateSubjects" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_GradeCertificateSubjects_GradeCertificates_CertificateId"
                        FOREIGN KEY ("CertificateId") REFERENCES "GradeCertificates" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_GradeCertificateSubjects_Courses_CourseId"
                        FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS "IX_GradeCertificateSubjects_TenantId" ON "GradeCertificateSubjects" ("TenantId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_GradeCertificateSubjects_CertificateId_CourseId"
                    ON "GradeCertificateSubjects" ("CertificateId", "CourseId");
                CREATE INDEX IF NOT EXISTS "IX_GradeCertificateSubjects_CourseId" ON "GradeCertificateSubjects" ("CourseId");

                CREATE TABLE IF NOT EXISTS "GradeCertificateMarks" (
                    "Id" uuid NOT NULL,
                    "TenantId" character varying(64) NULL,
                    "SubjectId" uuid NOT NULL,
                    "StudentId" uuid NOT NULL,
                    "Degree" numeric(7,2) NOT NULL,
                    "UpdatedByUserId" uuid NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_GradeCertificateMarks" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_GradeCertificateMarks_GradeCertificateSubjects_SubjectId"
                        FOREIGN KEY ("SubjectId") REFERENCES "GradeCertificateSubjects" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_GradeCertificateMarks_Users_StudentId"
                        FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS "IX_GradeCertificateMarks_TenantId" ON "GradeCertificateMarks" ("TenantId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_GradeCertificateMarks_SubjectId_StudentId"
                    ON "GradeCertificateMarks" ("SubjectId", "StudentId");
                CREATE INDEX IF NOT EXISTS "IX_GradeCertificateMarks_StudentId" ON "GradeCertificateMarks" ("StudentId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "GradeCertificateMarks";
                DROP TABLE IF EXISTS "GradeCertificateSubjects";
                DROP TABLE IF EXISTS "GradeCertificates";
                """);
        }
    }
}
