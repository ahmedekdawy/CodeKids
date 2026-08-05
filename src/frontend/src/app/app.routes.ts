import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { StudentHomeComponent } from './pages/student-home/student-home.component';
import { LessonPlayComponent } from './pages/lesson-play/lesson-play.component';
import { QuizPlayComponent } from './pages/quiz-play/quiz-play.component';
import { AssignmentPlayComponent } from './pages/assignment-play/assignment-play.component';
import { ParentDashboardComponent } from './pages/parent-dashboard/parent-dashboard.component';
import { AdminShellComponent } from './pages/admin/admin-shell.component';
import { AdminUsersComponent } from './pages/admin/admin-users.component';
import { AdminStudentsComponent } from './pages/admin/admin-students.component';
import { AdminCoursesComponent } from './pages/admin/admin-courses.component';
import { AdminCreateClassroomComponent } from './pages/admin/admin-create-classroom.component';
import { AdminAssignClassroomComponent } from './pages/admin/admin-assign-classroom.component';
import { AdminEnrollStudentComponent } from './pages/admin/admin-enroll-student.component';
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
import { ExamPlayComponent } from './pages/exam-play/exam-play.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: 'student',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: StudentHomeComponent
  },
  {
    path: 'lessons/:lessonId',
    canActivate: [authGuard, roleGuard(['Student'])],
    component: LessonPlayComponent
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
    path: 'teacher',
    canActivate: [authGuard, roleGuard(['Teacher'])],
    component: TeacherShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      { path: 'overview', component: TeacherOverviewComponent },
      { path: 'videos', component: TeacherVideosComponent },
      { path: 'zoom', component: TeacherZoomComponent },
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
      { path: '', pathMatch: 'full', redirectTo: 'users' },
      { path: 'users', component: AdminUsersComponent },
      { path: 'students', component: AdminStudentsComponent },
      { path: 'courses', component: AdminCoursesComponent },
      { path: 'create-classroom', component: AdminCreateClassroomComponent },
      { path: 'assign-classroom', component: AdminAssignClassroomComponent },
      { path: 'enroll-student', component: AdminEnrollStudentComponent },
      { path: 'classrooms', redirectTo: 'create-classroom' }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
