import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PanelNavItem, PanelShellComponent } from '../../layouts/panel-shell/panel-shell.component';

@Component({
  selector: 'app-admin-shell',
  imports: [PanelShellComponent, RouterOutlet],
  template: `
    <app-panel-shell
      titleKey="shell.admin.title"
      subtitleKey="shell.admin.subtitle"
      [navItems]="navItems">
      <router-outlet />
    </app-panel-shell>
  `
})
export class AdminShellComponent {
  readonly navItems: PanelNavItem[] = [
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

    { labelKey: 'nav.admin.teacherAssessments', path: '/admin/teacher-assessments', icon: 'Q', categoryKey: 'nav.cat.assessments' },
    { labelKey: 'nav.admin.weeklyReports', path: '/admin/weekly-reports', icon: 'W', categoryKey: 'nav.cat.assessments' },

    { labelKey: 'nav.admin.timetable', path: '/admin/timetable', icon: 'H', categoryKey: 'nav.cat.schedule' },
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
}
