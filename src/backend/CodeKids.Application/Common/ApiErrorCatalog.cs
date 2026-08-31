using System.Text.RegularExpressions;

namespace CodeKids.Application.Common;

public static class ApiErrorCatalog
{
    private static readonly Dictionary<string, string> Exact = new(StringComparer.Ordinal)
    {
        ["An account with that email already exists."] = "api.errors.auth.emailExists",
        ["Tenant name is required."] = "api.errors.tenant.nameRequired",
        ["A valid tenant email is required."] = "api.errors.tenant.emailRequired",
        ["Display name is required."] = "api.errors.tenant.displayNameRequired",
        ["Verification token is invalid or has expired."] = "api.errors.tenant.verifyTokenInvalid",
        ["Public registration is limited to Student or Parent. Teachers are created by Super Admin."] = "api.errors.auth.registrationRoleLimited",
        ["Parent account was not found."] = "api.errors.auth.parentNotFound",
        ["Invalid email or password."] = "api.errors.auth.invalidCredentials",
        ["Invalid email, mobile, or password."] = "api.errors.auth.invalidCredentials",
        ["An account with that mobile number already exists."] = "api.errors.auth.mobileExists",
        ["Email or mobile is required."] = "api.errors.auth.emailOrMobileRequired",
        ["Reset token is invalid or has expired."] = "api.errors.auth.resetTokenInvalid",
        ["Password must be at least 6 characters."] = "api.errors.auth.passwordTooShort",
        ["Teacher work shift must be Am, Pm, or Both."] = "api.errors.user.workShiftInvalid",
        ["Student school type must be Arabic or Language."] = "api.errors.user.schoolTypeInvalid",
        ["Teacher contract type must be Session or Monthly."] = "api.errors.user.contractTypeInvalid",
        ["Payment amounts cannot be negative."] = "api.errors.user.amountNegative",
        ["Each subject rate must use a unique course."] = "api.errors.user.rateCourseUnique",
        ["One or more courses were not found for teacher rates."] = "api.errors.user.rateCourseNotFound",
        ["Each subject rate needs a session amount or monthly salary."] = "api.errors.user.rateAmountRequired",
        ["Student not found."] = "api.errors.student.notFound",
        ["Classroom not found."] = "api.errors.classroom.notFound",
        ["Classroom name is required."] = "api.errors.classroom.nameRequired",
        ["Teacher not found."] = "api.errors.teacher.notFound",
        ["Course not found."] = "api.errors.course.notFound",
        ["Appointment not found."] = "api.errors.appointment.notFound",
        ["End time must be after start time."] = "api.errors.appointment.endAfterStart",
        ["Selected user must be a teacher."] = "api.errors.appointment.mustBeTeacher",
        ["Teacher already has an appointment in this time slot."] = "api.errors.appointment.overlap",
        ["Repeat until date is required for weekly recurrence."] = "api.errors.appointment.repeatUntilRequired",
        ["Repeat until date must be on or after the first session."] = "api.errors.appointment.repeatUntilBeforeStart",
        ["Weekly recurrence exceeds the maximum number of sessions."] = "api.errors.appointment.repeatTooMany",
        ["Timetable entry not found."] = "api.errors.timetable.notFound",
        ["Day of week must be between 0 (Sunday) and 6 (Saturday)."] = "api.errors.timetable.dayInvalid",
        ["Period must be am or pm."] = "api.errors.timetable.periodInvalid",
        ["Teacher already has a timetable session in this slot."] = "api.errors.timetable.overlap",
        ["A course with the same school type (or All) is already in this timetable slot."] = "api.errors.timetable.schoolTypeOverlap",
        ["Session attendance not found."] = "api.errors.attendance.notFound",
        ["Session date is required."] = "api.errors.attendance.dateRequired",
        ["Attendance for this teacher, course, and date already exists."] = "api.errors.attendance.duplicate",
        ["You can only remove your own attendance records."] = "api.errors.attendance.notOwner",
        ["End date must be on or after the start date."] = "api.errors.attendance.dateRangeInvalid",
        ["Stage must be between 0 and 3."] = "api.errors.attendance.stageInvalid",
        ["Tuition payment not found."] = "api.errors.payment.notFound",
        ["Select either a parent or a student without a parent."] = "api.errors.payment.payerRequired",
        ["Payment year is invalid."] = "api.errors.payment.yearInvalid",
        ["Payment month must be between 1 and 12."] = "api.errors.payment.monthInvalid",
        ["Payment amount must be greater than zero."] = "api.errors.payment.amountInvalid",
        ["Payment date is required."] = "api.errors.payment.dateRequired",
        ["Parent not found."] = "api.errors.parent.notFound",
        ["Selected user must be a parent."] = "api.errors.payment.mustBeParent",
        ["Selected user must be a student."] = "api.errors.payment.mustBeStudent",
        ["This student has a parent; record the payment under the parent."] = "api.errors.payment.studentHasParent",
        ["Other expense not found."] = "api.errors.expense.notFound",
        ["Expense name is required."] = "api.errors.expense.nameRequired",
        ["Expense name is too long."] = "api.errors.expense.nameTooLong",
        ["Expense amount must be greater than zero."] = "api.errors.expense.amountInvalid",
        ["Expense date is required."] = "api.errors.expense.dateRequired",
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
        ["Only the quiz teacher can review attempts."] = "api.errors.quiz.teacherOnlyReview",
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
        ["You can only edit the course tree for courses assigned to you."] = "api.errors.courseTree.teacherOnlyEdit",
        ["Zoom OAuth code and state are required."] = "api.errors.zoom.oauthRequired",
        ["Invalid or expired Zoom OAuth state."] = "api.errors.zoom.oauthStateInvalid",
        ["Zoom OAuth Client ID is required."] = "api.errors.zoom.clientIdRequired",
        ["Zoom OAuth Client Secret is required."] = "api.errors.zoom.clientSecretRequired",
        ["Zoom OAuth redirect URI is required."] = "api.errors.zoom.redirectRequired",
        ["Only MP4, WebM, and MOV videos are allowed."] = "api.errors.media.videoTypeInvalid",
        ["Video URL is required."] = "api.errors.media.urlRequired",
        ["Video URL is too long."] = "api.errors.media.urlTooLong",
        ["Video URL must be an absolute http or https link."] = "api.errors.media.urlInvalid",
        ["Lesson not found."] = "api.errors.lesson.notFound",
        ["Unit not found."] = "api.errors.unit.notFound",
        ["Ask scope must be course, unit, or lesson."] = "api.errors.studentAsk.scopeInvalid",
        ["Question is required."] = "api.errors.studentAsk.questionRequired",
        ["Question is too long."] = "api.errors.studentAsk.questionTooLong",
        ["Student Ask is not enabled for this course, unit, or lesson."] = "api.errors.studentAsk.notEnabled",
        ["You do not have access to this course."] = "api.errors.studentAsk.noAccess",
        ["Select a course, unit, or lesson to ask about."] = "api.errors.studentAsk.scopeRequired",
        ["Lesson does not belong to the selected course."] = "api.errors.studentAsk.lessonCourseMismatch",
        ["Lesson does not belong to the selected unit."] = "api.errors.studentAsk.lessonUnitMismatch",
        ["Unit does not belong to the selected course."] = "api.errors.studentAsk.unitCourseMismatch",
        ["Asked question not found."] = "api.errors.studentAsk.questionNotFound",
        ["You can only delete your own questions."] = "api.errors.studentAsk.deleteOwnOnly",
        ["Answer is required."] = "api.errors.studentAsk.answerRequired",
        ["Answer is too long."] = "api.errors.studentAsk.answerTooLong",
        ["You can only chat in classrooms and courses assigned to you."] = "api.errors.chat.teacherOnly",
        ["You are not in this chat."] = "api.errors.chat.notMember",
        ["You are blocked from this chat."] = "api.errors.chat.blocked",
        ["Only teachers can delete chat messages."] = "api.errors.chat.teacherOnlyDelete",
        ["Only teachers can block students from chat."] = "api.errors.chat.teacherOnlyBlock",
        ["Chat message not found."] = "api.errors.chat.messageNotFound",
        ["Student is not in this chat."] = "api.errors.chat.studentNotInChat",
        ["You can only block students."] = "api.errors.chat.blockStudentsOnly",
        ["Select at least one student."] = "api.errors.chat.studentsRequired",
        ["Select one student for a direct chat."] = "api.errors.chat.directOneStudent",
        ["Select at least two students for a group chat."] = "api.errors.chat.groupMinStudents",
        ["Chat kind must be Direct, Group, or Class."] = "api.errors.chat.kindInvalid",
        ["Message is too long."] = "api.errors.chat.messageTooLong",
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
        ["Stage was not found."] = "api.errors.course.stageNotFound",
        ["Selected grades must belong to the chosen stage."] = "api.errors.course.gradeStageMismatch",
        ["Course school type must be Arabic, Language, or All."] = "api.errors.course.schoolTypeInvalid",
        ["Super Admin account not found."] = "api.errors.admin.superAdminNotFound",
        ["User not found."] = "api.errors.admin.userNotFound",
        ["Cannot demote the last Super Admin."] = "api.errors.admin.cannotDemoteLastSuperAdmin",
        ["You cannot delete your own account."] = "api.errors.admin.cannotDeleteSelf",
        ["Cannot delete the last Super Admin."] = "api.errors.admin.cannotDeleteLastSuperAdmin",
        ["Parent account not found."] = "api.errors.parent.notFound",
        ["This student is not linked to your account."] = "api.errors.parent.childNotLinked",
        ["Student is not in your classrooms."] = "api.errors.analytics.studentNotInClassrooms",
        ["Study plan not found."] = "api.errors.studyPlan.notFound",
        ["From and to dates are required."] = "api.errors.studyPlan.datesRequired",
        ["Study plan range cannot exceed 14 days."] = "api.errors.studyPlan.rangeTooLong",
        ["Study plan cannot exceed 20 weeks."] = "api.errors.studyPlan.rangeTooLong",
        ["A study plan already exists for this course and start date."] = "api.errors.studyPlan.duplicate",
        ["Course is not assigned to this teacher."] = "api.errors.studyPlan.courseNotAssigned",
        ["Could not generate a study plan."] = "api.errors.studyPlan.generateFailed",
        ["Terabox session expired and could not be refreshed automatically. Log in at terabox.com, complete verification, and update Ndus/JsToken in server config."] = "api.errors.media.teraboxSessionExpired"
    };

    private static readonly (Regex Pattern, string Code)[] Patterns =
    [
        (new Regex("^Question '(.+)' belongs to a different course\\.$", RegexOptions.Compiled), "api.errors.exam.questionWrongCourse"),
        (new Regex("^File size must be between 1 byte and (\\d+) bytes\\.$", RegexOptions.Compiled), "api.errors.media.fileSizeInvalid"),
        (new Regex("^Session number must be between 1 and (\\d+)\\.$", RegexOptions.Compiled), "api.errors.timetable.sessionInvalid"),
        (new Regex("^Timetable session count must be between (\\d+) and (\\d+)\\.$", RegexOptions.Compiled), "api.errors.site.sessionCountInvalid"),
        (new Regex("^PM start time must be between (.+) and (.+)\\.$", RegexOptions.Compiled), "api.errors.site.pmStartInvalid"),
        (new Regex("^Cannot reduce AM sessions while timetable entries exist beyond session (\\d+)\\.$", RegexOptions.Compiled), "api.errors.site.amSessionCountInUse"),
        (new Regex("^Cannot reduce PM sessions while timetable entries exist beyond session (\\d+)\\.$", RegexOptions.Compiled), "api.errors.site.pmSessionCountInUse")
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
            if (patternCode == "api.errors.timetable.sessionInvalid" && match.Groups.Count > 1)
                args["max"] = match.Groups[1].Value;
            if (patternCode == "api.errors.site.sessionCountInvalid" && match.Groups.Count > 2)
            {
                args["min"] = match.Groups[1].Value;
                args["max"] = match.Groups[2].Value;
            }
            if (patternCode == "api.errors.site.pmStartInvalid" && match.Groups.Count > 2)
            {
                args["min"] = match.Groups[1].Value;
                args["max"] = match.Groups[2].Value;
            }
            if ((patternCode is "api.errors.site.amSessionCountInUse" or "api.errors.site.pmSessionCountInUse") && match.Groups.Count > 1)
                args["max"] = match.Groups[1].Value;

            return (patternCode, args);
        }

        return null;
    }
}
