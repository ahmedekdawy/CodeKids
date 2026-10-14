import { PanelNavItem } from './layouts/panel-shell/panel-shell.component';
import { TenantStyle, navItemsForStyle } from './tenant-style';

export const ADMIN_NAV_ITEMS: PanelNavItem[] = [
  { labelKey: 'nav.admin.dashboard', path: '/admin/dashboard', icon: 'D' },

  { labelKey: 'nav.admin.admins', path: '/admin/admins', icon: 'A', categoryKey: 'nav.cat.people' },
  { labelKey: 'nav.admin.teachers', path: '/admin/teachers', icon: 'T', categoryKey: 'nav.cat.people' },
  { labelKey: 'nav.admin.parents', path: '/admin/parents', icon: 'P', categoryKey: 'nav.cat.people' },
  { labelKey: 'nav.admin.students', path: '/admin/students', icon: 'S', categoryKey: 'nav.cat.people' },

  { labelKey: 'nav.admin.courses', path: '/admin/courses', icon: 'C', categoryKey: 'nav.cat.content' },
  { labelKey: 'nav.admin.courseTree', path: '/admin/course-tree', icon: 'U', categoryKey: 'nav.cat.content' },
  { labelKey: 'nav.admin.videos', path: '/admin/videos', icon: 'V', categoryKey: 'nav.cat.content' },
  { labelKey: 'nav.admin.studyPlans', path: '/admin/study-plans', icon: 'L', categoryKey: 'nav.cat.content' },

  { labelKey: 'nav.admin.classrooms', path: '/admin/create-classroom', icon: 'R', categoryKey: 'nav.cat.classrooms' },
  { labelKey: 'nav.admin.assign', path: '/admin/assign-classroom', icon: 'G', categoryKey: 'nav.cat.classrooms' },
  { labelKey: 'nav.admin.enroll', path: '/admin/enroll-student', icon: 'E', categoryKey: 'nav.cat.classrooms' },

  { labelKey: 'nav.admin.smartAssistant', path: '/admin/smart-assistant', icon: 'M', categoryKey: 'nav.cat.content' },

  { labelKey: 'nav.admin.teacherAssessments', path: '/admin/teacher-assessments', icon: 'Q', categoryKey: 'nav.cat.assessments' },
  { labelKey: 'nav.admin.weeklyReports', path: '/admin/weekly-reports', icon: 'W', categoryKey: 'nav.cat.assessments' },
  { labelKey: 'nav.admin.gradeCertificates', path: '/admin/grade-certificates', icon: 'D', categoryKey: 'nav.cat.assessments' },

  { labelKey: 'nav.admin.timetable', path: '/admin/timetable', icon: 'H', categoryKey: 'nav.cat.schedule' },
  { labelKey: 'nav.availability', path: '/admin/availability', icon: 'V', categoryKey: 'nav.cat.schedule' },
  { labelKey: 'nav.admin.appointments', path: '/admin/appointments', icon: 'K', categoryKey: 'nav.cat.schedule' },
  { labelKey: 'nav.admin.attendance', path: '/admin/attendance', icon: 'N', categoryKey: 'nav.cat.schedule' },
  { labelKey: 'nav.admin.studentAttendance', path: '/admin/student-attendance', icon: 'A', categoryKey: 'nav.cat.schedule' },

  { labelKey: 'nav.admin.payroll', path: '/admin/payroll', icon: '$', categoryKey: 'nav.cat.finance' },
  { labelKey: 'nav.admin.payments', path: '/admin/payments', icon: 'F', categoryKey: 'nav.cat.finance' },
  { labelKey: 'nav.admin.expenses', path: '/admin/other-expenses', icon: 'X', categoryKey: 'nav.cat.finance' },
  { labelKey: 'nav.admin.accountReport', path: '/admin/account-report', icon: '%', categoryKey: 'nav.cat.finance' },

  { labelKey: 'nav.admin.whatsapp', path: '/admin/whatsapp', icon: 'W', categoryKey: 'nav.cat.system' },
  { labelKey: 'nav.admin.settings', path: '/admin/site-settings', icon: 'B', categoryKey: 'nav.cat.system' }
];

export const TEACHER_NAV_ITEMS: PanelNavItem[] = [
  { labelKey: 'nav.teacher.overview', path: '/teacher/overview', icon: 'O' },
  { labelKey: 'nav.teacher.account', path: '/teacher/account', icon: '👤' },

  { labelKey: 'nav.cat.content', path: '/teacher/videos', icon: 'V', categoryKey: 'nav.cat.content' },
  { labelKey: 'nav.teacher.materials', path: '/teacher/materials', icon: 'M', categoryKey: 'nav.cat.content' },
  { labelKey: 'nav.teacher.smartAssistant', path: '/teacher/smart-study-assistant', icon: '🤖', categoryKey: 'nav.cat.content' },
  { labelKey: 'nav.teacher.courseTree', path: '/teacher/course-tree', icon: 'U', categoryKey: 'nav.cat.content' },
  { labelKey: 'nav.teacher.studyPlans', path: '/teacher/study-plans', icon: 'P', categoryKey: 'nav.cat.content' },

  { labelKey: 'nav.teacher.exams', path: '/teacher/exams', icon: 'E', categoryKey: 'nav.cat.assessments' },
  { labelKey: 'nav.teacher.quizzes', path: '/teacher/quizzes', icon: 'Q', categoryKey: 'nav.cat.assessments' },
  { labelKey: 'nav.teacher.assignments', path: '/teacher/assignments', icon: 'A', categoryKey: 'nav.cat.assessments' },
  { labelKey: 'nav.teacher.questionBank', path: '/teacher/question-bank', icon: 'B', categoryKey: 'nav.cat.assessments' },
  { labelKey: 'nav.teacher.review', path: '/teacher/review', icon: 'R', categoryKey: 'nav.cat.assessments' },

  { labelKey: 'nav.teacher.timetable', path: '/teacher/timetable', icon: 'H', categoryKey: 'nav.cat.schedule' },
  { labelKey: 'nav.availability', path: '/teacher/availability', icon: 'V', categoryKey: 'nav.cat.schedule' },
  { labelKey: 'nav.teacher.appointments', path: '/teacher/appointments', icon: 'K', categoryKey: 'nav.cat.schedule' },
  { labelKey: 'nav.teacher.attendance', path: '/teacher/attendance', icon: 'N', categoryKey: 'nav.cat.schedule' },
  { labelKey: 'nav.teacher.studentAttendance', path: '/teacher/student-attendance', icon: 'A', categoryKey: 'nav.cat.schedule' },

  { labelKey: 'nav.teacher.students', path: '/teacher/students', icon: 'S', categoryKey: 'nav.cat.communication' },
  { labelKey: 'nav.teacher.askedQuestions', path: '/teacher/asked-questions', icon: '?', categoryKey: 'nav.cat.communication' },
  { labelKey: 'nav.teacher.chat', path: '/teacher/chat', icon: 'C', categoryKey: 'nav.cat.communication' },
  { labelKey: 'nav.teacher.weeklyReports', path: '/teacher/weekly-reports', icon: 'W', categoryKey: 'nav.cat.communication' },
  { labelKey: 'nav.teacher.gradeCertificates', path: '/teacher/grade-certificates', icon: 'D', categoryKey: 'nav.cat.assessments' }
];

/**
 * Admin pages the owner of a single-teacher tenant does not need in their combined menu: either the
 * teacher panel already has the same page, or the page is about managing other teachers.
 */
const OWNER_SKIPPED_ADMIN_PATHS = [
  '/admin/course-tree',
  '/admin/videos',
  '/admin/study-plans',
  '/admin/smart-assistant',
  '/admin/teacher-assessments',
  '/admin/weekly-reports',
  '/admin/grade-certificates',
  '/admin/timetable',
  '/admin/appointments',
  '/admin/student-attendance',
  '/admin/availability'
];

/**
 * Menu for a panel. The owner of a single-teacher tenant gets one combined menu (their teacher pages
 * plus the admin pages), shown in both panels so it does not change as they move between them.
 */
export function panelNavItems(panel: 'admin' | 'teacher', style: TenantStyle, isTenantOwner: boolean): PanelNavItem[] {
  if (isTenantOwner) {
    return navItemsForStyle(
      [...TEACHER_NAV_ITEMS, ...ADMIN_NAV_ITEMS.filter((item) => !OWNER_SKIPPED_ADMIN_PATHS.includes(item.path))],
      style
    );
  }

  return navItemsForStyle(panel === 'admin' ? ADMIN_NAV_ITEMS : TEACHER_NAV_ITEMS, style);
}
