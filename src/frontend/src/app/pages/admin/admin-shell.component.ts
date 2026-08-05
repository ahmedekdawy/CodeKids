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
    { labelKey: 'nav.admin.users', path: '/admin/users', icon: 'U' },
    { labelKey: 'nav.admin.students', path: '/admin/students', icon: 'S' },
    { labelKey: 'nav.admin.courses', path: '/admin/courses', icon: 'C' },
    { labelKey: 'nav.admin.classrooms', path: '/admin/create-classroom', icon: 'R' },
    { labelKey: 'nav.admin.assign', path: '/admin/assign-classroom', icon: 'A' },
    { labelKey: 'nav.admin.enroll', path: '/admin/enroll-student', icon: 'E' }
  ];
}
