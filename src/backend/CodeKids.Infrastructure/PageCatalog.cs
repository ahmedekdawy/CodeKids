namespace CodeKids.Infrastructure;

/// <summary>
/// The fixed catalogue of navigable pages. A user's menu is built from these rows, constrained by the
/// pages their roles expose. Keep this list in sync with the frontend routes and nav labels.
/// </summary>
public static class PageCatalog
{
    public sealed record Definition(
        Guid Id,
        string Key,
        string LabelKey,
        string Icon,
        string CategoryKey,
        string Panel,
        int SortOrder);

    public const string AdminPanel = "admin";
    public const string TeacherPanel = "teacher";

    public static readonly IReadOnlyList<Definition> All =
    [
        // Teacher panel.
        Teacher(1, "/teacher/overview", "nav.teacher.overview", "O", ""),
        Teacher(2, "/teacher/menu", "nav.teacher.menu", "☰", ""),
        Teacher(3, "/teacher/account", "nav.teacher.account", "👤", ""),
        Teacher(4, "/teacher/videos", "nav.cat.content", "V", "nav.cat.content"),
        Teacher(5, "/teacher/materials", "nav.teacher.materials", "M", "nav.cat.content"),
        Teacher(6, "/teacher/smart-study-assistant", "nav.teacher.smartAssistant", "🤖", "nav.cat.content"),
        Teacher(7, "/teacher/course-tree", "nav.teacher.courseTree", "U", "nav.cat.content"),
        Teacher(8, "/teacher/study-plans", "nav.teacher.studyPlans", "P", "nav.cat.content"),
        Teacher(9, "/teacher/exams", "nav.teacher.exams", "E", "nav.cat.assessments"),
        Teacher(10, "/teacher/quizzes", "nav.teacher.quizzes", "Q", "nav.cat.assessments"),
        Teacher(11, "/teacher/assignments", "nav.teacher.assignments", "A", "nav.cat.assessments"),
        Teacher(12, "/teacher/question-bank", "nav.teacher.questionBank", "B", "nav.cat.assessments"),
        Teacher(13, "/teacher/review", "nav.teacher.review", "R", "nav.cat.assessments"),
        Teacher(14, "/teacher/timetable", "nav.teacher.timetable", "H", "nav.cat.schedule"),
        Teacher(15, "/teacher/availability", "nav.availability", "V", "nav.cat.schedule"),
        Teacher(16, "/teacher/appointments", "nav.teacher.appointments", "K", "nav.cat.schedule"),
        Teacher(17, "/teacher/attendance", "nav.teacher.attendance", "N", "nav.cat.schedule"),
        Teacher(18, "/teacher/student-attendance", "nav.teacher.studentAttendance", "A", "nav.cat.schedule"),
        Teacher(19, "/teacher/students", "nav.teacher.students", "S", "nav.cat.communication"),
        Teacher(20, "/teacher/asked-questions", "nav.teacher.askedQuestions", "?", "nav.cat.communication"),
        Teacher(21, "/teacher/chat", "nav.teacher.chat", "C", "nav.cat.communication"),
        Teacher(22, "/teacher/weekly-reports", "nav.teacher.weeklyReports", "W", "nav.cat.communication"),
        Teacher(23, "/teacher/grade-certificates", "nav.teacher.gradeCertificates", "D", "nav.cat.assessments"),

        // Admin panel.
        Admin(1, "/admin/dashboard", "nav.admin.dashboard", "D", ""),
        Admin(2, "/admin/admins", "nav.admin.admins", "A", "nav.cat.people"),
        Admin(3, "/admin/teachers", "nav.admin.teachers", "T", "nav.cat.people"),
        Admin(4, "/admin/parents", "nav.admin.parents", "P", "nav.cat.people"),
        Admin(5, "/admin/students", "nav.admin.students", "S", "nav.cat.people"),
        Admin(6, "/admin/courses", "nav.admin.courses", "C", "nav.cat.content"),
        Admin(7, "/admin/course-tree", "nav.admin.courseTree", "U", "nav.cat.content"),
        Admin(8, "/admin/videos", "nav.admin.videos", "V", "nav.cat.content"),
        Admin(9, "/admin/study-plans", "nav.admin.studyPlans", "L", "nav.cat.content"),
        Admin(10, "/admin/create-classroom", "nav.admin.classrooms", "R", "nav.cat.classrooms"),
        Admin(11, "/admin/assign-classroom", "nav.admin.assign", "G", "nav.cat.classrooms"),
        Admin(12, "/admin/enroll-student", "nav.admin.enroll", "E", "nav.cat.classrooms"),
        Admin(13, "/admin/smart-assistant", "nav.admin.smartAssistant", "M", "nav.cat.content"),
        Admin(14, "/admin/teacher-assessments", "nav.admin.teacherAssessments", "Q", "nav.cat.assessments"),
        Admin(15, "/admin/weekly-reports", "nav.admin.weeklyReports", "W", "nav.cat.assessments"),
        Admin(16, "/admin/grade-certificates", "nav.admin.gradeCertificates", "D", "nav.cat.assessments"),
        Admin(17, "/admin/timetable", "nav.admin.timetable", "H", "nav.cat.schedule"),
        Admin(18, "/admin/availability", "nav.availability", "V", "nav.cat.schedule"),
        Admin(19, "/admin/appointments", "nav.admin.appointments", "K", "nav.cat.schedule"),
        Admin(20, "/admin/attendance", "nav.admin.attendance", "N", "nav.cat.schedule"),
        Admin(21, "/admin/student-attendance", "nav.admin.studentAttendance", "A", "nav.cat.schedule"),
        Admin(22, "/admin/payroll", "nav.admin.payroll", "$", "nav.cat.finance"),
        Admin(23, "/admin/payments", "nav.admin.payments", "F", "nav.cat.finance"),
        Admin(24, "/admin/other-expenses", "nav.admin.expenses", "X", "nav.cat.finance"),
        Admin(25, "/admin/account-report", "nav.admin.accountReport", "%", "nav.cat.finance"),
        Admin(26, "/admin/whatsapp", "nav.admin.whatsapp", "W", "nav.cat.system"),
        Admin(27, "/admin/roles", "nav.admin.roles", "R", "nav.cat.system"),
        Admin(28, "/admin/menu", "nav.admin.menu", "☰", "nav.cat.system"),
        Admin(29, "/admin/site-settings", "nav.admin.settings", "B", "nav.cat.system")
    ];

    private static Definition Teacher(int index, string key, string labelKey, string icon, string categoryKey) =>
        new(
            Guid.Parse($"33333333-3333-3333-3333-{index:D12}"),
            key,
            labelKey,
            icon,
            categoryKey,
            TeacherPanel,
            1000 + index);

    private static Definition Admin(int index, string key, string labelKey, string icon, string categoryKey) =>
        new(
            Guid.Parse($"33333333-3333-3333-3333-{100 + index:D12}"),
            key,
            labelKey,
            icon,
            categoryKey,
            AdminPanel,
            2000 + index);
}
