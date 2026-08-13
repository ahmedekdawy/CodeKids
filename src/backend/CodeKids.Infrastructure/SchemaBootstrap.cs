using Microsoft.EntityFrameworkCore;

namespace CodeKids.Infrastructure;

public static class SchemaBootstrap
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        // Create core tables that may be missing on partially provisioned databases
        // (EnsureCreated skips when any tables already exist).
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "Avatars" (
                "Id" uuid NOT NULL,
                "Name" character varying(80) NOT NULL,
                "Theme" character varying(60) NOT NULL,
                "AccentColor" character varying(20) NOT NULL,
                "Emoji" character varying(16) NOT NULL,
                "UnlockXp" integer NOT NULL,
                CONSTRAINT "PK_Avatars" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "Users" (
                "Id" uuid NOT NULL,
                "Email" character varying(160) NOT NULL,
                "DisplayName" character varying(80) NOT NULL,
                "PasswordHash" character varying(200) NOT NULL,
                "Role" character varying(30) NOT NULL,
                "ParentId" uuid NULL,
                "AvatarId" uuid NULL,
                "MobilePhone" character varying(30) NOT NULL DEFAULT '',
                "ZoomAccessToken" character varying(2000) NOT NULL DEFAULT '',
                "ZoomRefreshToken" character varying(2000) NOT NULL DEFAULT '',
                "ZoomTokenExpiresAt" timestamp with time zone NULL,
                "ZoomConnectedEmail" character varying(160) NOT NULL DEFAULT '',
                "TotalXp" integer NOT NULL DEFAULT 0,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email") WHERE "Email" <> '';
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_MobilePhone" ON "Users" ("MobilePhone") WHERE "MobilePhone" <> '';

            CREATE TABLE IF NOT EXISTS "Courses" (
                "Id" uuid NOT NULL,
                "Title" character varying(120) NOT NULL,
                "Theme" character varying(60) NOT NULL,
                "Description" character varying(500) NOT NULL,
                "AgeMin" integer NOT NULL,
                "AgeMax" integer NOT NULL,
                "Term" character varying(20) NULL,
                "Grade" integer NULL,
                "SortOrder" integer NOT NULL,
                CONSTRAINT "PK_Courses" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "Lessons" (
                "Id" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "Title" character varying(120) NOT NULL,
                "Theme" character varying(60) NOT NULL,
                "Description" character varying(500) NOT NULL,
                "Difficulty" integer NOT NULL,
                "XpReward" integer NOT NULL,
                "SortOrder" integer NOT NULL,
                CONSTRAINT "PK_Lessons" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "LessonSteps" (
                "Id" uuid NOT NULL,
                "LessonId" uuid NOT NULL,
                "Title" character varying(120) NOT NULL,
                "Prompt" character varying(500) NOT NULL,
                "ExpectedAnswer" character varying(120) NOT NULL,
                "StepNumber" integer NOT NULL,
                CONSTRAINT "PK_LessonSteps" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "StudentProgress" (
                "Id" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "LessonId" uuid NOT NULL,
                "StepId" uuid NOT NULL,
                "IsCompleted" boolean NOT NULL,
                "EarnedXp" integer NOT NULL,
                "CompletedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_StudentProgress" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_StudentProgress_UserId_StepId" ON "StudentProgress" ("UserId", "StepId");

            CREATE TABLE IF NOT EXISTS "Badges" (
                "Id" uuid NOT NULL,
                "Code" character varying(40) NOT NULL,
                "Name" character varying(120) NOT NULL,
                "Description" character varying(400) NOT NULL,
                "Icon" character varying(16) NOT NULL,
                "RequiredXp" integer NOT NULL,
                "RequiredSteps" integer NOT NULL,
                CONSTRAINT "PK_Badges" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Badges_Code" ON "Badges" ("Code");

            CREATE TABLE IF NOT EXISTS "UserBadges" (
                "Id" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "BadgeId" uuid NOT NULL,
                "AwardedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_UserBadges" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "Quizzes" (
                "Id" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "ClassroomId" uuid NULL,
                "CreatedByUserId" uuid NULL,
                "Title" character varying(120) NOT NULL,
                "Description" character varying(400) NOT NULL,
                "XpReward" integer NOT NULL,
                CONSTRAINT "PK_Quizzes" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "QuizQuestions" (
                "Id" uuid NOT NULL,
                "QuizId" uuid NOT NULL,
                "Prompt" character varying(300) NOT NULL,
                "OptionA" character varying(200) NOT NULL,
                "OptionB" character varying(200) NOT NULL,
                "OptionC" character varying(200) NOT NULL,
                "OptionsJson" character varying(8000) NOT NULL DEFAULT '[]',
                "CorrectOption" character varying(40) NOT NULL,
                "SortOrder" integer NOT NULL,
                CONSTRAINT "PK_QuizQuestions" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "QuizAttempts" (
                "Id" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "QuizId" uuid NOT NULL,
                "Score" integer NOT NULL,
                "TotalQuestions" integer NOT NULL,
                "EarnedXp" integer NOT NULL,
                "CompletedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_QuizAttempts" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "Classrooms" (
                "Id" uuid NOT NULL,
                "Name" character varying(120) NOT NULL,
                "Description" character varying(500) NOT NULL,
                "TeacherId" uuid NULL,
                "CourseId" uuid NULL,
                "WhatsAppGroupInviteUrl" character varying(500) NOT NULL,
                "WhatsAppNotifyPhones" character varying(1000) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_Classrooms" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "ClassroomCourses" (
                "Id" uuid NOT NULL,
                "ClassroomId" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "TeacherId" uuid NOT NULL,
                "AssignedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_ClassroomCourses" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "ClassroomStudents" (
                "Id" uuid NOT NULL,
                "ClassroomId" uuid NOT NULL,
                "StudentId" uuid NOT NULL,
                "JoinedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_ClassroomStudents" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "Assignments" (
                "Id" uuid NOT NULL,
                "ClassroomId" uuid NOT NULL,
                "CreatedByUserId" uuid NOT NULL,
                "Title" character varying(160) NOT NULL,
                "Description" character varying(800) NOT NULL,
                "DueAtUtc" timestamp with time zone NULL,
                "XpReward" integer NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_Assignments" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "AssignmentQuestions" (
                "Id" uuid NOT NULL,
                "AssignmentId" uuid NOT NULL,
                "Prompt" character varying(500) NOT NULL,
                "QuestionType" character varying(30) NOT NULL,
                "OptionA" character varying(120) NULL,
                "OptionB" character varying(120) NULL,
                "OptionC" character varying(120) NULL,
                "CorrectAnswer" character varying(200) NOT NULL,
                "Points" integer NOT NULL,
                "SortOrder" integer NOT NULL,
                CONSTRAINT "PK_AssignmentQuestions" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "AssignmentSubmissions" (
                "Id" uuid NOT NULL,
                "AssignmentId" uuid NOT NULL,
                "StudentId" uuid NOT NULL,
                "Status" character varying(20) NOT NULL,
                "Score" integer NULL,
                "MaxScore" integer NULL,
                "TeacherFeedback" character varying(800) NULL,
                "SubmittedAtUtc" timestamp with time zone NOT NULL,
                "GradedAtUtc" timestamp with time zone NULL,
                CONSTRAINT "PK_AssignmentSubmissions" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "AssignmentAnswers" (
                "Id" uuid NOT NULL,
                "SubmissionId" uuid NOT NULL,
                "QuestionId" uuid NOT NULL,
                "AnswerText" character varying(1000) NOT NULL,
                "IsCorrect" boolean NULL,
                "PointsAwarded" integer NULL,
                CONSTRAINT "PK_AssignmentAnswers" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "LiveSessions" (
                "Id" uuid NOT NULL,
                "Title" character varying(160) NOT NULL,
                "Description" character varying(500) NOT NULL,
                "HostUserId" uuid NOT NULL,
                "CourseId" uuid NULL,
                "ClassroomId" uuid NULL,
                "StartsAtUtc" timestamp with time zone NOT NULL,
                "DurationMinutes" integer NOT NULL,
                "ZoomMeetingId" character varying(40) NOT NULL,
                "JoinUrl" character varying(500) NOT NULL,
                "StartUrl" character varying(1000) NOT NULL,
                "WhatsAppNotified" boolean NOT NULL DEFAULT FALSE,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_LiveSessions" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "Appointments" (
                "Id" uuid NOT NULL,
                "TeacherId" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "StartsAtUtc" timestamp with time zone NOT NULL,
                "EndsAtUtc" timestamp with time zone NOT NULL,
                "Notes" character varying(500) NOT NULL DEFAULT '',
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_Appointments" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "FixedTimetableEntries" (
                "Id" uuid NOT NULL,
                "TeacherId" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "DayOfWeek" integer NOT NULL,
                "SessionNumber" integer NOT NULL,
                "Period" character varying(10) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_FixedTimetableEntries" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "TeacherSessionAttendances" (
                "Id" uuid NOT NULL,
                "TeacherId" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "SessionDate" date NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_TeacherSessionAttendances" PRIMARY KEY ("Id")
            );

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

            CREATE TABLE IF NOT EXISTS "OtherExpenses" (
                "Id" uuid NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Amount" numeric(18,2) NOT NULL,
                "ExpenseDate" date NOT NULL,
                "Notes" character varying(500) NOT NULL DEFAULT '',
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_OtherExpenses" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "BankQuestions" (
                "Id" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "CreatedByUserId" uuid NOT NULL,
                "ParentQuestionId" uuid NULL,
                "QuestionType" character varying(30) NOT NULL,
                "Prompt" character varying(4000) NOT NULL,
                "PassageText" character varying(4000) NOT NULL,
                "OptionA" character varying(200) NULL,
                "OptionB" character varying(200) NULL,
                "OptionC" character varying(200) NULL,
                "OptionD" character varying(200) NULL,
                "CorrectAnswer" character varying(200) NOT NULL,
                "Points" integer NOT NULL,
                "SortOrder" integer NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_BankQuestions" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "Exams" (
                "Id" uuid NOT NULL,
                "ClassroomId" uuid NOT NULL,
                "CourseId" uuid NULL,
                "CreatedByUserId" uuid NOT NULL,
                "Title" character varying(160) NOT NULL,
                "Description" character varying(800) NOT NULL,
                "DueAtUtc" timestamp with time zone NULL,
                "XpReward" integer NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_Exams" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "ExamQuestions" (
                "Id" uuid NOT NULL,
                "ExamId" uuid NOT NULL,
                "BankQuestionId" uuid NULL,
                "ParentExamQuestionId" uuid NULL,
                "QuestionType" character varying(30) NOT NULL,
                "Prompt" character varying(4000) NOT NULL,
                "PassageText" character varying(4000) NOT NULL,
                "OptionA" character varying(200) NULL,
                "OptionB" character varying(200) NULL,
                "OptionC" character varying(200) NULL,
                "OptionD" character varying(200) NULL,
                "CorrectAnswer" character varying(200) NOT NULL,
                "Points" integer NOT NULL,
                "SortOrder" integer NOT NULL,
                CONSTRAINT "PK_ExamQuestions" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "ExamAttempts" (
                "Id" uuid NOT NULL,
                "ExamId" uuid NOT NULL,
                "StudentId" uuid NOT NULL,
                "Status" character varying(20) NOT NULL,
                "Score" integer NULL,
                "MaxScore" integer NULL,
                "TeacherFeedback" character varying(800) NULL,
                "SubmittedAtUtc" timestamp with time zone NOT NULL,
                "GradedAtUtc" timestamp with time zone NULL,
                CONSTRAINT "PK_ExamAttempts" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "ExamAnswers" (
                "Id" uuid NOT NULL,
                "AttemptId" uuid NOT NULL,
                "ExamQuestionId" uuid NOT NULL,
                "AnswerText" character varying(1000) NOT NULL,
                "IsCorrect" boolean NULL,
                "PointsAwarded" integer NULL,
                CONSTRAINT "PK_ExamAnswers" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "MediaAssets" (
                "Id" uuid NOT NULL,
                "StorageKey" character varying(400) NOT NULL,
                "ExternalUrl" character varying(1000) NULL,
                "FileName" character varying(260) NOT NULL,
                "ContentType" character varying(120) NOT NULL,
                "SizeBytes" bigint NOT NULL,
                "DurationSeconds" integer NULL,
                "UploadedByUserId" uuid NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_MediaAssets" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "LessonVideos" (
                "Id" uuid NOT NULL,
                "LessonId" uuid NOT NULL,
                "MediaAssetId" uuid NOT NULL,
                "Title" character varying(160) NOT NULL,
                "SortOrder" integer NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_LessonVideos" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "VideoWatchSessions" (
                "Id" uuid NOT NULL,
                "MediaAssetId" uuid NOT NULL,
                "StudentId" uuid NOT NULL,
                "LessonId" uuid NULL,
                "ActualWatchSeconds" integer NOT NULL,
                "MaxPositionSeconds" integer NOT NULL,
                "UsedSpeedUp" boolean NOT NULL DEFAULT FALSE,
                "SkippedAhead" boolean NOT NULL DEFAULT FALSE,
                "StartedAtUtc" timestamp with time zone NOT NULL,
                "LastEventAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_VideoWatchSessions" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "WhatsAppReportLogs" (
                "Id" uuid NOT NULL,
                "ClassroomId" uuid NULL,
                "StudentId" uuid NULL,
                "ReportType" character varying(60) NOT NULL,
                "RecipientPhone" character varying(30) NOT NULL,
                "Status" character varying(40) NOT NULL,
                "MessagePreview" character varying(1000) NOT NULL,
                "SentAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_WhatsAppReportLogs" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "SiteSettings" (
                "Id" uuid NOT NULL,
                "SiteName" character varying(120) NOT NULL,
                "LogoStorageKey" character varying(400) NOT NULL DEFAULT '',
                "LogoContentType" character varying(120) NOT NULL DEFAULT '',
                "BannerStorageKey" character varying(400) NOT NULL DEFAULT '',
                "BannerContentType" character varying(120) NOT NULL DEFAULT '',
                "UpdatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_SiteSettings" PRIMARY KEY ("Id")
            );
            """,
            cancellationToken);

        // Additive column upgrades (safe when tables exist)
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "Quizzes" ADD COLUMN IF NOT EXISTS "ClassroomId" uuid NULL;
            ALTER TABLE "Quizzes" ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL;
            ALTER TABLE "LiveSessions" ADD COLUMN IF NOT EXISTS "ClassroomId" uuid NULL;
            ALTER TABLE "LiveSessions" ADD COLUMN IF NOT EXISTS "WhatsAppNotified" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "MobilePhone" character varying(30) NOT NULL DEFAULT '';
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "Grade" integer NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "WorkShift" character varying(20) NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "Stages" character varying(40) NOT NULL DEFAULT '';
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ContractType" character varying(20) NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PrimaryAmount" numeric(18,2) NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PrepAmount" numeric(18,2) NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SecondaryAmount" numeric(18,2) NULL;
            ALTER TABLE "Users" DROP COLUMN IF EXISTS "SpecialSessionAmount";

            CREATE TABLE IF NOT EXISTS "TeacherCourseRates" (
                "Id" uuid NOT NULL,
                "TeacherId" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "SessionAmount" numeric(18,2) NULL,
                "MonthlySalary" numeric(18,2) NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_TeacherCourseRates" PRIMARY KEY ("Id")
            );
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_TeacherCourseRates_Users_TeacherId'
                ) THEN
                    ALTER TABLE "TeacherCourseRates"
                        ADD CONSTRAINT "FK_TeacherCourseRates_Users_TeacherId"
                        FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE CASCADE;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_TeacherCourseRates_Courses_CourseId'
                ) THEN
                    ALTER TABLE "TeacherCourseRates"
                        ADD CONSTRAINT "FK_TeacherCourseRates_Courses_CourseId"
                        FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE;
                END IF;
            END $$;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_TeacherCourseRates_TeacherId_CourseId"
                ON "TeacherCourseRates" ("TeacherId", "CourseId");
            CREATE INDEX IF NOT EXISTS "IX_TeacherCourseRates_CourseId"
                ON "TeacherCourseRates" ("CourseId");
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ZoomAccessToken" character varying(2000) NOT NULL DEFAULT '';
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ZoomRefreshToken" character varying(2000) NOT NULL DEFAULT '';
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ZoomTokenExpiresAt" timestamp with time zone NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ZoomConnectedEmail" character varying(160) NOT NULL DEFAULT '';
            -- Allow multiple empty emails/phones; uniqueness only when set.
            DROP INDEX IF EXISTS "IX_Users_Email";
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email") WHERE "Email" <> '';
            DROP INDEX IF EXISTS "IX_Users_MobilePhone";
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_MobilePhone" ON "Users" ("MobilePhone") WHERE "MobilePhone" <> '';
            ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Term" character varying(20) NOT NULL DEFAULT 'FullYear';
            ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Grade" integer NOT NULL DEFAULT 1;

            CREATE TABLE IF NOT EXISTS "ClassroomCourses" (
                "Id" uuid NOT NULL,
                "ClassroomId" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "TeacherId" uuid NOT NULL,
                "AssignedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_ClassroomCourses" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_ClassroomCourses_ClassroomId_CourseId" ON "ClassroomCourses" ("ClassroomId", "CourseId");
            CREATE INDEX IF NOT EXISTS "IX_ClassroomCourses_TeacherId" ON "ClassroomCourses" ("TeacherId");
            CREATE INDEX IF NOT EXISTS "IX_ClassroomStudents_ClassroomId_StudentId" ON "ClassroomStudents" ("ClassroomId", "StudentId");

            -- Backfill one course+teacher link from legacy single CourseId + TeacherId.
            INSERT INTO "ClassroomCourses" ("Id", "ClassroomId", "CourseId", "TeacherId", "AssignedAtUtc")
            SELECT gen_random_uuid(),
                   c."Id",
                   c."CourseId",
                   c."TeacherId",
                   COALESCE(c."CreatedAtUtc", NOW())
            FROM "Classrooms" c
            WHERE c."CourseId" IS NOT NULL
              AND c."TeacherId" IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM "ClassroomCourses" cc
                  WHERE cc."ClassroomId" = c."Id" AND cc."CourseId" = c."CourseId"
              );

            -- Teachers now live on ClassroomCourses only.
            DROP TABLE IF EXISTS "ClassroomTeachers";

            CREATE INDEX IF NOT EXISTS "IX_AssignmentSubmissions_AssignmentId_StudentId" ON "AssignmentSubmissions" ("AssignmentId", "StudentId");
            CREATE INDEX IF NOT EXISTS "IX_LiveSessions_StartsAtUtc" ON "LiveSessions" ("StartsAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_Appointments_StartsAtUtc" ON "Appointments" ("StartsAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_Appointments_TeacherId_StartsAtUtc" ON "Appointments" ("TeacherId", "StartsAtUtc");
            DROP INDEX IF EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber";
            DROP INDEX IF EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNum~";
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_TeacherId_DayOfWeek_Period_SessionNumber_CourseId"
                ON "FixedTimetableEntries" ("TeacherId", "DayOfWeek", "Period", "SessionNumber", "CourseId");
            CREATE INDEX IF NOT EXISTS "IX_FixedTimetableEntries_DayOfWeek_Period_SessionNumber"
                ON "FixedTimetableEntries" ("DayOfWeek", "Period", "SessionNumber");

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_TeacherSessionAttendances_Users_TeacherId'
                ) THEN
                    ALTER TABLE "TeacherSessionAttendances"
                        ADD CONSTRAINT "FK_TeacherSessionAttendances_Users_TeacherId"
                        FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_TeacherSessionAttendances_Courses_CourseId'
                ) THEN
                    ALTER TABLE "TeacherSessionAttendances"
                        ADD CONSTRAINT "FK_TeacherSessionAttendances_Courses_CourseId"
                        FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE RESTRICT;
                END IF;
            END $$;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_TeacherSessionAttendances_TeacherId_CourseId_SessionDate"
                ON "TeacherSessionAttendances" ("TeacherId", "CourseId", "SessionDate");
            CREATE INDEX IF NOT EXISTS "IX_TeacherSessionAttendances_SessionDate"
                ON "TeacherSessionAttendances" ("SessionDate");

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

            CREATE INDEX IF NOT EXISTS "IX_OtherExpenses_ExpenseDate"
                ON "OtherExpenses" ("ExpenseDate");
            CREATE INDEX IF NOT EXISTS "IX_OtherExpenses_Name"
                ON "OtherExpenses" ("Name");

            CREATE INDEX IF NOT EXISTS "IX_BankQuestions_CourseId_CreatedByUserId" ON "BankQuestions" ("CourseId", "CreatedByUserId");
            CREATE INDEX IF NOT EXISTS "IX_ExamAttempts_ExamId_StudentId" ON "ExamAttempts" ("ExamId", "StudentId");
            ALTER TABLE "MediaAssets" ADD COLUMN IF NOT EXISTS "ExternalUrl" character varying(1000) NULL;

            CREATE INDEX IF NOT EXISTS "IX_LessonVideos_LessonId" ON "LessonVideos" ("LessonId");
            CREATE INDEX IF NOT EXISTS "IX_VideoWatchSessions_MediaAssetId_StudentId" ON "VideoWatchSessions" ("MediaAssetId", "StudentId");

            UPDATE "BankQuestions" SET "QuestionType" = 'Paragraph' WHERE "QuestionType" = 'UnderlineParagraph';
            UPDATE "ExamQuestions" SET "QuestionType" = 'Paragraph' WHERE "QuestionType" = 'UnderlineParagraph';
            ALTER TABLE "BankQuestions" ALTER COLUMN "Prompt" TYPE character varying(4000);
            ALTER TABLE "ExamQuestions" ALTER COLUMN "Prompt" TYPE character varying(4000);

            ALTER TABLE "Assignments" ADD COLUMN IF NOT EXISTS "SolutionVideoMediaAssetId" uuid NULL;
            ALTER TABLE "AssignmentSubmissions" ADD COLUMN IF NOT EXISTS "StartedAtUtc" timestamp with time zone NULL;
            ALTER TABLE "ExamAttempts" ADD COLUMN IF NOT EXISTS "StartedAtUtc" timestamp with time zone NOT NULL DEFAULT NOW();
            ALTER TABLE "ExamAttempts" ALTER COLUMN "SubmittedAtUtc" DROP NOT NULL;
            ALTER TABLE "BankQuestions" ADD COLUMN IF NOT EXISTS "LessonId" uuid NULL;
            ALTER TABLE "ExamQuestions" ADD COLUMN IF NOT EXISTS "LessonId" uuid NULL;
            ALTER TABLE "Classrooms" ADD COLUMN IF NOT EXISTS "DailyWhatsAppReportsEnabled" boolean NOT NULL DEFAULT TRUE;
            ALTER TABLE "Classrooms" ADD COLUMN IF NOT EXISTS "Grade" integer NULL;
            ALTER TABLE "BankQuestions" ADD COLUMN IF NOT EXISTS "OptionsJson" character varying(8000) NOT NULL DEFAULT '[]';
            ALTER TABLE "ExamQuestions" ADD COLUMN IF NOT EXISTS "OptionsJson" character varying(8000) NOT NULL DEFAULT '[]';
            ALTER TABLE "QuizQuestions" ADD COLUMN IF NOT EXISTS "OptionsJson" character varying(8000) NOT NULL DEFAULT '[]';

            ALTER TABLE "Courses" ALTER COLUMN "Term" DROP NOT NULL;
            ALTER TABLE "Courses" ALTER COLUMN "Grade" DROP NOT NULL;

            CREATE TABLE IF NOT EXISTS "CourseUnits" (
                "Id" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "Title" character varying(120) NOT NULL,
                "Description" character varying(500) NOT NULL,
                "SortOrder" integer NOT NULL,
                CONSTRAINT "PK_CourseUnits" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_CourseUnits_CourseId_SortOrder"
                ON "CourseUnits" ("CourseId", "SortOrder");
            DO $$ BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_CourseUnits_Courses_CourseId'
                ) THEN
                    ALTER TABLE "CourseUnits"
                        ADD CONSTRAINT "FK_CourseUnits_Courses_CourseId"
                        FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE;
                END IF;
            END $$;

            ALTER TABLE "Lessons" ADD COLUMN IF NOT EXISTS "UnitId" uuid NULL;
            CREATE INDEX IF NOT EXISTS "IX_Lessons_UnitId" ON "Lessons" ("UnitId");
            DO $$ BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_Lessons_CourseUnits_UnitId'
                ) THEN
                    ALTER TABLE "Lessons"
                        ADD CONSTRAINT "FK_Lessons_CourseUnits_UnitId"
                        FOREIGN KEY ("UnitId") REFERENCES "CourseUnits" ("Id") ON DELETE CASCADE;
                END IF;
            END $$;

            -- Backfill: one default unit per course for lessons that have no unit yet.
            INSERT INTO "CourseUnits" ("Id", "CourseId", "Title", "Description", "SortOrder")
            SELECT gen_random_uuid(), c."Id", 'Unit 1', '', 1
            FROM "Courses" c
            WHERE EXISTS (
                SELECT 1 FROM "Lessons" l
                WHERE l."CourseId" = c."Id" AND l."UnitId" IS NULL
            )
            AND NOT EXISTS (
                SELECT 1 FROM "CourseUnits" u WHERE u."CourseId" = c."Id"
            );

            UPDATE "Lessons" l
            SET "UnitId" = u."Id"
            FROM (
                SELECT DISTINCT ON ("CourseId") "Id", "CourseId"
                FROM "CourseUnits"
                ORDER BY "CourseId", "SortOrder", "Title"
            ) u
            WHERE l."CourseId" = u."CourseId" AND l."UnitId" IS NULL;
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "StudentCourseEnrollments" (
                "Id" uuid NOT NULL,
                "StudentId" uuid NOT NULL,
                "ClassroomId" uuid NOT NULL,
                "CourseId" uuid NOT NULL,
                "EnrolledAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_StudentCourseEnrollments" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_StudentCourseEnrollments_StudentId_ClassroomId_CourseId"
                ON "StudentCourseEnrollments" ("StudentId", "ClassroomId", "CourseId");
            CREATE INDEX IF NOT EXISTS "IX_StudentCourseEnrollments_ClassroomId"
                ON "StudentCourseEnrollments" ("ClassroomId");
            CREATE INDEX IF NOT EXISTS "IX_StudentCourseEnrollments_CourseId"
                ON "StudentCourseEnrollments" ("CourseId");
            DO $$ BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentCourseEnrollments_Users_StudentId'
                ) THEN
                    ALTER TABLE "StudentCourseEnrollments"
                        ADD CONSTRAINT "FK_StudentCourseEnrollments_Users_StudentId"
                        FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE CASCADE;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentCourseEnrollments_Classrooms_ClassroomId'
                ) THEN
                    ALTER TABLE "StudentCourseEnrollments"
                        ADD CONSTRAINT "FK_StudentCourseEnrollments_Classrooms_ClassroomId"
                        FOREIGN KEY ("ClassroomId") REFERENCES "Classrooms" ("Id") ON DELETE CASCADE;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_StudentCourseEnrollments_Courses_CourseId'
                ) THEN
                    ALTER TABLE "StudentCourseEnrollments"
                        ADD CONSTRAINT "FK_StudentCourseEnrollments_Courses_CourseId"
                        FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE;
                END IF;
            END $$;
            """,
            cancellationToken);
    }
}
