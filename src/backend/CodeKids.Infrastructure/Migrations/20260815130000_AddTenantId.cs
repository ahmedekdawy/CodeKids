using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeKids.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260815130000_AddTenantId")]
    public partial class AddTenantId : Migration
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
                        'Users','Avatars','Courses','CourseUnits','Lessons','LessonSteps','StudentProgress',
                        'Quizzes','QuizQuestions','QuizAttempts','Badges','UserBadges','LiveSessions',
                        'Appointments','FixedTimetableEntries','TeacherSessionAttendances','TuitionPayments',
                        'OtherExpenses','TeacherCourseRates','PasswordResetTokens','Classrooms','ClassroomCourses',
                        'ClassroomStudents','StudentCourseEnrollments','Assignments','AssignmentQuestions',
                        'AssignmentSubmissions','AssignmentAnswers','BankQuestions','Exams','ExamQuestions',
                        'ExamAttempts','ExamAnswers','MediaAssets','LessonVideos','VideoWatchSessions',
                        'WhatsAppReportLogs','SiteSettings'
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

                DROP INDEX IF EXISTS "IX_Users_Email";
                DROP INDEX IF EXISTS "IX_Users_MobilePhone";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_TenantId_Email"
                    ON "Users" ("TenantId", "Email") WHERE "Email" <> '';
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_TenantId_MobilePhone"
                    ON "Users" ("TenantId", "MobilePhone") WHERE "MobilePhone" <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Users_TenantId_Email";
                DROP INDEX IF EXISTS "IX_Users_TenantId_MobilePhone";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email") WHERE "Email" <> '';
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_MobilePhone" ON "Users" ("MobilePhone") WHERE "MobilePhone" <> '';
                """);
        }
    }
}
