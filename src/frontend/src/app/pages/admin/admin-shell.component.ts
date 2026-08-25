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
    { labelKey: 'nav.admin.admins', path: '/admin/admins', icon: 'A' },
    { labelKey: 'nav.admin.teachers', path: '/admin/teachers', icon: 'T' },
    { labelKey: 'nav.admin.parents', path: '/admin/parents', icon: 'P' },
    { labelKey: 'nav.admin.students', path: '/admin/students', icon: 'S' },
    { labelKey: 'nav.admin.courses', path: '/admin/courses', icon: 'C' },
    { labelKey: 'nav.admin.courseTree', path: '/admin/course-tree', icon: 'U' },
    { labelKey: 'nav.admin.classrooms', path: '/admin/create-classroom', icon: 'R' },
    { labelKey: 'nav.admin.assign', path: '/admin/assign-classroom', icon: 'G' },
    { labelKey: 'nav.admin.enroll', path: '/admin/enroll-student', icon: 'E' },
    { labelKey: 'nav.admin.appointments', path: '/admin/appointments', icon: 'K' },
    { labelKey: 'nav.admin.timetable', path: '/admin/timetable', icon: 'H' },
    { labelKey: 'nav.admin.studyPlans', path: '/admin/study-plans', icon: 'L' },
    { labelKey: 'nav.admin.attendance', path: '/admin/attendance', icon: 'N' },
    { labelKey: 'nav.admin.payroll', path: '/admin/payroll', icon: '$' },
    { labelKey: 'nav.admin.accountReport', path: '/admin/account-report', icon: '%' },
    { labelKey: 'nav.admin.payments', path: '/admin/payments', icon: 'F' },
    { labelKey: 'nav.admin.expenses', path: '/admin/other-expenses', icon: 'X' },
    { labelKey: 'nav.admin.settings', path: '/admin/site-settings', icon: 'B' }
  ];
}
