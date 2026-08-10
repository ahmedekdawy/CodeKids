using System.Text.RegularExpressions;

namespace CodeKids.Application.Common;

public static class ApiErrorCatalog
{
    private static readonly Dictionary<string, string> Exact = new(StringComparer.Ordinal)
    {
        ["An account with that email already exists."] = "api.errors.auth.emailExists",
        ["Public registration is limited to Student or Parent. Teachers are created by Super Admin."] = "api.errors.auth.registrationRoleLimited",
        ["Parent account was not found."] = "api.errors.auth.parentNotFound",
        ["Invalid email or password."] = "api.errors.auth.invalidCredentials",
        ["Invalid email, mobile, or password."] = "api.errors.auth.invalidCredentials",
        ["An account with that mobile number already exists."] = "api.errors.auth.mobileExists",
        ["Email or mobile is required."] = "api.errors.auth.emailOrMobileRequired",
        ["Teacher work shift must be Am, Pm, or Both."] = "api.errors.user.workShiftInvalid",
        ["Student not found."] = "api.errors.student.notFound",
        ["Classroom not found."] = "api.errors.classroom.notFound",
        ["Classroom name is required."] = "api.errors.classroom.nameRequired",
        ["Teacher not found."] = "api.errors.teacher.notFound",
        ["Course not found."] = "api.errors.course.notFound",
        ["Appointment not found."] = "api.errors.appointment.notFound",
        ["End time must be after start time."] = "api.errors.appointment.endAfterStart",
        ["Selected user must be a teacher."] = "api.errors.appointment.mustBeTeacher",
        ["Teacher already has an appointment in this time slot."] = "api.errors.appointment.overlap",
        ["Message is required."] = "api.errors.classroom.messageRequired",
        ["Only the assigned classroom teacher can message this class."] = "api.errors.classroom.teacherOnlyMessage",
        ["Student is not enrolled in this classroom."] = "api.errors.classroom.studentNotEnrolled",
        ["Student is not in this classroom."] = "api.errors.classroom.studentNotInClassroom",
        ["Only the assigned classroom teacher can create exams."] = "api.errors.exam.teacherOnlyCreate",
        ["Exam title is required."] = "api.errors.exam.titleRequired",
        ["Select at least one bank question."] = "api.errors.exam.questionsRequired",
        ["One or more question IDs were not found (use parent question IDs only)."] = "api.errors.exam.questionsNotFound",
        ["You can only use your own bank questions in an exam."] = "api.errors.exam.ownQuestionsOnly",
        ["Exam not found."] = "api.errors.exam.notFound",
        ["Exam already submitted."] = "api.errors.exam.alreadySubmitted",
        ["Only the classroom teacher can review exam attempts."] = "api.errors.exam.teacherOnlyReview",
        ["Assignment not found."] = "api.errors.assignment.notFound",
        ["Only the assigned classroom teacher can create assignments."] = "api.errors.assignment.teacherOnlyCreate",
        ["Assignment title is required."] = "api.errors.assignment.titleRequired",
        ["Add at least one question."] = "api.errors.assignment.questionsRequired",
        ["Question type must be ShortAnswer or MultipleChoice."] = "api.errors.assignment.questionTypeInvalid",
        ["Assignment already submitted."] = "api.errors.assignment.alreadySubmitted",
        ["Only the classroom teacher can review submissions."] = "api.errors.assignment.teacherOnlyReview",
        ["Submission not found."] = "api.errors.assignment.submissionNotFound",
        ["Only the classroom teacher can grade submissions."] = "api.errors.assignment.teacherOnlyGrade",
        ["Quiz not found."] = "api.errors.quiz.notFound",
        ["Only the assigned classroom teacher can create quizzes for that classroom."] = "api.errors.quiz.teacherOnlyCreate",
        ["Teacher must be assigned to a classroom before creating quizzes."] = "api.errors.quiz.teacherNeedsClassroom",
        ["Each quiz question needs at least two options."] = "api.errors.quiz.minOptions",
        ["Correct option must match one of the listed choices."] = "api.errors.quiz.correctOptionRequired",
        ["Question prompt is required."] = "api.errors.questionBank.promptRequired",
        ["Underline questions require the sentence/text to underline in."] = "api.errors.questionBank.underlineTextRequired",
        ["Underline questions require the correct underlined phrase."] = "api.errors.questionBank.underlinePhraseRequired",
        ["Correct answer is required."] = "api.errors.questionBank.correctAnswerRequired",
        ["True/False correct answer must be True or False."] = "api.errors.questionBank.trueFalseInvalid",
        ["At least two answer options are required."] = "api.errors.questionBank.minOptions",
        ["Select a correct answer from the options list."] = "api.errors.questionBank.selectCorrect",
        ["Correct answer must be one of the listed options."] = "api.errors.questionBank.correctMustBeOption",
        ["MultiChoice correct answers must be among the listed options."] = "api.errors.questionBank.multiCorrectInvalid",
        ["Lesson not found for this course."] = "api.errors.questionBank.lessonNotFound",
        ["Paragraph questions require passage text."] = "api.errors.questionBank.passageRequired",
        ["Paragraph questions need at least one child question."] = "api.errors.questionBank.childRequired",
        ["Only Paragraph questions can have child questions."] = "api.errors.questionBank.childOnlyParagraph",
        ["Child questions cannot be Paragraph or Underline."] = "api.errors.questionBank.childTypeInvalid",
        ["Only assigned teachers can add bank questions."] = "api.errors.questionBank.teacherOnlyAdd",
        ["Bank question not found."] = "api.errors.questionBank.notFound",
        ["You can only edit your own bank questions."] = "api.errors.questionBank.ownEditOnly",
        ["Edit the parent Paragraph question instead."] = "api.errors.questionBank.editParentInstead",
        ["You can only delete your own bank questions."] = "api.errors.questionBank.ownDeleteOnly",
        ["Delete the parent Paragraph question instead."] = "api.errors.questionBank.deleteParentInstead",
        ["Title is required."] = "api.errors.meeting.titleRequired",
        ["Duration must be between 15 and 240 minutes."] = "api.errors.meeting.durationRange",
        ["Start time must be in the future."] = "api.errors.meeting.startInFuture",
        ["Teacher account not found."] = "api.errors.teacher.accountNotFound",
        ["You can only schedule Zoom meetings for classrooms assigned to you."] = "api.errors.meeting.teacherOnlySchedule",
        ["Zoom OAuth code and state are required."] = "api.errors.zoom.oauthRequired",
        ["Invalid or expired Zoom OAuth state."] = "api.errors.zoom.oauthStateInvalid",
        ["Zoom OAuth Client ID is required."] = "api.errors.zoom.clientIdRequired",
        ["Zoom OAuth Client Secret is required."] = "api.errors.zoom.clientSecretRequired",
        ["Zoom OAuth redirect URI is required."] = "api.errors.zoom.redirectRequired",
        ["Only MP4, WebM, and MOV videos are allowed."] = "api.errors.media.videoTypeInvalid",
        ["Lesson not found."] = "api.errors.lesson.notFound",
        ["Media asset not found."] = "api.errors.media.notFound",
        ["You can only attach media you uploaded."] = "api.errors.media.ownUploadOnly",
        ["Only the classroom teacher can attach a solution video."] = "api.errors.media.teacherOnlySolution",
        ["Invalid storage key."] = "api.errors.media.invalidStorageKey",
        ["Expected multipart form upload."] = "api.errors.media.multipartRequired",
        ["No file uploaded."] = "api.errors.media.noFile",
        ["Role must be Teacher, Student, Parent, or SuperAdmin."] = "api.errors.admin.roleInvalid",
        ["Course title is required."] = "api.errors.course.titleRequired",
        ["Term must be FirstTerm, SecondTerm, or FullYear."] = "api.errors.course.termInvalid",
        ["Grade must be KG1, KG2, or between 1 and 12."] = "api.errors.course.gradeInvalid",
        ["Super Admin account not found."] = "api.errors.admin.superAdminNotFound",
        ["User not found."] = "api.errors.admin.userNotFound",
        ["Cannot demote the last Super Admin."] = "api.errors.admin.cannotDemoteLastSuperAdmin",
        ["You cannot delete your own account."] = "api.errors.admin.cannotDeleteSelf",
        ["Cannot delete the last Super Admin."] = "api.errors.admin.cannotDeleteLastSuperAdmin",
        ["Parent account not found."] = "api.errors.parent.notFound",
        ["Student is not in your classrooms."] = "api.errors.analytics.studentNotInClassrooms"
    };

    private static readonly (Regex Pattern, string Code)[] Patterns =
    [
        (new Regex("^Question '(.+)' belongs to a different course\\.$", RegexOptions.Compiled), "api.errors.exam.questionWrongCourse"),
        (new Regex("^File size must be between 1 byte and (\\d+) bytes\\.$", RegexOptions.Compiled), "api.errors.media.fileSizeInvalid")
    ];

    public static (string Code, Dictionary<string, string> Args)? TryResolve(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;

        if (Exact.TryGetValue(message, out var code))
            return (code, new Dictionary<string, string>());

        foreach (var (pattern, patternCode) in Patterns)
        {
            var match = pattern.Match(message);
            if (!match.Success) continue;

            var args = new Dictionary<string, string>();
            if (patternCode == "api.errors.exam.questionWrongCourse" && match.Groups.Count > 1)
                args["prompt"] = match.Groups[1].Value;
            if (patternCode == "api.errors.media.fileSizeInvalid" && match.Groups.Count > 1)
                args["maxBytes"] = match.Groups[1].Value;

            return (patternCode, args);
        }

        return null;
    }
}
