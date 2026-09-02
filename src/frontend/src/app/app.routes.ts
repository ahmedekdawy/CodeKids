import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { RegisterTenantComponent } from './pages/register-tenant/register-tenant.component';
import { VerifyTenantComponent } from './pages/verify-tenant/verify-tenant.component';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './pages/reset-password/reset-password.component';
import { StudentHomeComponent } from './pages/student-home/student-home.component';
import { LessonPlayComponent } from './pages/lesson-play/lesson-play.component';
import { QuizPlayComponent } from './pages/quiz-play/quiz-play.component';
import { AssignmentPlayComponent } from './pages/assignment-play/assignment-play.component';
import { ParentDashboardComponent } from './pages/parent-dashboard/parent-dashboard.component';
import { StudyPlanViewComponent } from './pages/study-plan-view/study-plan-view.component';
import { AdminShellComponent } from './pages/admin/admin-shell.component';
import { AdminDashboardComponent } from './pages/admin/admin-dashboard.component';
import { AdminUsersComponent } from './pages/admin/admin-users.component';
import { AdminStudentsComponent } from './pages/admin/admin-students.component';
import { AdminCoursesComponent } from './pages/admin/admin-courses.component';
import { AdminCourseTreeComponent } from './pages/admin/admin-course-tree.component';
import { AdminCreateClassroomComponent } from './pages/admin/admin-create-classroom.component';
import { AdminAssignClassroomComponent } from './pages/admin/admin-assign-classroom.component';
import { AdminEnrollStudentComponent } from './pages/admin/admin-enroll-student.component';
import { AdminSiteSettingsComponent } from './pages/admin/admin-site-settings.component';
import { AdminAppointmentsComponent } from './pages/admin/admin-appointments.component';
import { AdminTimetableComponent } from './pages/admin/admin-timetable.component';
import { AdminStudyPlansComponent } from './pages/admin/admin-study-plans.component';
import { AdminAttendanceComponent } from './pages/admin/admin-attendance.component';
import { AdminStudentAttendanceComponent } from './pages/admin/admin-student-attendance.component';
import { AdminPayrollComponent } from './pages/admin/admin-payroll.component';
import { AdminAccountReportComponent } from './pages/admin/admin-account-report.component';
import { AdminPaymentsComponent } from './pages/admin/admin-payments.component';
import { AdminOtherExpensesComponent } from './pages/admin/admin-other-expenses.component';
import { CoursePlayComponent } from './pages/course-play/course-play.component';
import { AdminVideosComponent } from './pages/admin/admin-videos.component';
import { TeacherShellComponent } from './pages/teacher/teacher-shell.component';
import { TeacherOverviewComponent } from './pages/teacher/teacher-overview.component';
import { TeacherZoomComponent } from './pages/teacher/teacher-zoom.component';
import { TeacherQuizzesComponent } from './pages/teacher/teacher-quizzes.component';
import { TeacherAssignmentsComponent } from './pages/teacher/teacher-assignments.component';
import { TeacherReviewComponent } from './pages/teacher/teacher-review.component';
import { TeacherStudentsComponent } from './pages/teacher/teacher-students.component';
import { TeacherQuestionBankComponent } from './pages/teacher/teacher-question-bank.component';
import { TeacherExamsComponent } from './pages/teacher/teacher-exams.component';
import { TeacherVideosComponent } from './pages/teacher/teacher-videos.component';
import { TeacherWhatsAppComponent } from './pages/teacher/teacher-whatsapp.component';
import { TeacherAppointmentsComponent } from './pages/teacher/teacher-appointments.component';
import { TeacherTimetableComponent } from './pages/teacher/teacher-timetable.component';
import { TeacherAttendanceComponent } from './pages/teacher/teacher-attendance.component';
import { TeacherStudentAttendanceComponent } from './pages/teacher/teacher-student-attendance.component';
import { TeacherWeeklyReportsComponent } from './pages/teacher/teacher-weekly-reports.component';
import { TeacherStudyPlansComponent } from './pages/teacher/teacher-study-plans.component';
import { TeacherAskedQuestionsComponent } from './pages/teacher/teacher-asked-questions.component';
import { TeacherChatComponent } from './pages/teacher/teacher-chat.component';
import { ExamPlayComponent } from './pages/exam-play/exam-play.component';
import { StudentAskedQuestionsComponent } from './pages/student-asked-questions/student-asked-questions.component';
import { StudentChatComponent } from './pages/student-chat/student-chat.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'register-tenant', component: RegisterTenantComponent },
  { path: 'verify-tenant', component: VerifyTenantComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  {
    path: 'student',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: StudentHomeComponent
  },
  {
    path: 'student/study-plans',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: StudyPlanViewComponent
  },
  {
    path: 'student/asked-questions',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: StudentAskedQuestionsComponent
  },
  {
    path: 'student/chat',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: StudentChatComponent
  },
  {
    path: 'lessons/:lessonId',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: LessonPlayComponent
  },
  {
    path: 'courses/:courseId',
    canActivate: [authGuard, roleGuard(['Student', 'Teacher', 'SuperAdmin'])],
    component: CoursePlayComponent
  },
  {
    path: 'quizzes/:quizId',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: QuizPlayComponent
  },
  {
    path: 'assignments/:assignmentId',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: AssignmentPlayComponent
  },
  {
    path: 'exams/:examId',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: ExamPlayComponent
  },
  {
    path: 'parent',
    canActivate: [authGuard, roleGuard(['Parent'])],
    component: ParentDashboardComponent
  },
  {
    path: 'parent/study-plans',
    canActivate: [authGuard, roleGuard(['Parent'])],
    component: StudyPlanViewComponent
  },
  {
    path: 'parent/study-plans/:studentId',
    canActivate: [authGuard, roleGuard(['Parent'])],
    component: StudyPlanViewComponent
  },
  {
    path: 'teacher',
    canActivate: [authGuard, roleGuard(['Teacher'])],
    component: TeacherShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      { path: 'overview', component: TeacherOverviewComponent },
      { path: 'videos', component: TeacherVideosComponent },
      { path: 'course-tree', component: AdminCourseTreeComponent },
      { path: 'asked-questions', component: TeacherAskedQuestionsComponent },
      { path: 'chat', component: TeacherChatComponent },
      { path: 'zoom', component: TeacherZoomComponent },
      { path: 'appointments', component: TeacherAppointmentsComponent },
      { path: 'timetable', component: TeacherTimetableComponent },
      { path: 'attendance', component: TeacherAttendanceComponent },
      { path: 'student-attendance', component: TeacherStudentAttendanceComponent },
      { path: 'weekly-reports', component: TeacherWeeklyReportsComponent },
      { path: 'study-plans', component: TeacherStudyPlansComponent },
      { path: 'whatsapp', component: TeacherWhatsAppComponent },
      { path: 'question-bank', component: TeacherQuestionBankComponent },
      { path: 'exams', component: TeacherExamsComponent },
      { path: 'quizzes', component: TeacherQuizzesComponent },
      { path: 'assignments', component: TeacherAssignmentsComponent },
      { path: 'review', component: TeacherReviewComponent },
      { path: 'students', component: TeacherStudentsComponent }
    ]
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard(['SuperAdmin'])],
    component: AdminShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: AdminDashboardComponent },
      { path: 'admins', component: AdminUsersComponent, data: { role: 'SuperAdmin' } },
      { path: 'teachers', component: AdminUsersComponent, data: { role: 'Teacher' } },
      { path: 'parents', component: AdminUsersComponent, data: { role: 'Parent' } },
      { path: 'users', redirectTo: 'admins' },
      { path: 'students', component: AdminStudentsComponent },
      { path: 'courses', component: AdminCoursesComponent },
      { path: 'course-tree', component: AdminCourseTreeComponent },
      { path: 'videos', component: AdminVideosComponent },
      { path: 'create-classroom', component: AdminCreateClassroomComponent },
      { path: 'assign-classroom', component: AdminAssignClassroomComponent },
      { path: 'enroll-student', component: AdminEnrollStudentComponent },
      { path: 'appointments', component: AdminAppointmentsComponent },
      { path: 'timetable', component: AdminTimetableComponent },
      { path: 'study-plans', component: AdminStudyPlansComponent },
      { path: 'attendance', component: AdminAttendanceComponent },
      { path: 'student-attendance', component: AdminStudentAttendanceComponent },
      { path: 'payroll', component: AdminPayrollComponent },
      { path: 'account-report', component: AdminAccountReportComponent },
      { path: 'payments', component: AdminPaymentsComponent },
      { path: 'other-expenses', component: AdminOtherExpensesComponent },
      { path: 'site-settings', component: AdminSiteSettingsComponent },
      { path: 'classrooms', redirectTo: 'create-classroom' }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
