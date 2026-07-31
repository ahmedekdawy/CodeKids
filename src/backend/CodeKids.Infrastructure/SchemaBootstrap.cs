using Microsoft.EntityFrameworkCore;

namespace CodeKids.Infrastructure;

public static class SchemaBootstrap
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "Quizzes" ADD COLUMN IF NOT EXISTS "ClassroomId" uuid NULL;
            ALTER TABLE "Quizzes" ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL;
            ALTER TABLE "LiveSessions" ADD COLUMN IF NOT EXISTS "ClassroomId" uuid NULL;
            ALTER TABLE "LiveSessions" ADD COLUMN IF NOT EXISTS "WhatsAppNotified" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "MobilePhone" character varying(30) NOT NULL DEFAULT '';
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ZoomAccessToken" character varying(2000) NOT NULL DEFAULT '';
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ZoomRefreshToken" character varying(2000) NOT NULL DEFAULT '';
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ZoomTokenExpiresAt" timestamp with time zone NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ZoomConnectedEmail" character varying(160) NOT NULL DEFAULT '';

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

            CREATE INDEX IF NOT EXISTS "IX_ClassroomStudents_ClassroomId_StudentId" ON "ClassroomStudents" ("ClassroomId", "StudentId");
            CREATE INDEX IF NOT EXISTS "IX_AssignmentSubmissions_AssignmentId_StudentId" ON "AssignmentSubmissions" ("AssignmentId", "StudentId");
            CREATE INDEX IF NOT EXISTS "IX_LiveSessions_StartsAtUtc" ON "LiveSessions" ("StartsAtUtc");
            """,
            cancellationToken);
    }
}
