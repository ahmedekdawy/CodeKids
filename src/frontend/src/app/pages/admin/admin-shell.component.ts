import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PanelNavItem, PanelShellComponent } from '../../layouts/panel-shell/panel-shell.component';

@Component({
  selector: 'app-admin-shell',
  imports: [PanelShellComponent, RouterOutlet],
  template: `
    <app-panel-shell
      title="Super Admin"
      subtitle="Platform control"
      [navItems]="navItems">
      <router-outlet />
    </app-panel-shell>
  `
})
export class AdminShellComponent {
  readonly navItems: PanelNavItem[] = [
    { label: 'Users', path: '/admin/users', icon: 'U' },
    { label: 'Students', path: '/admin/students', icon: 'S' },
    { label: 'Courses', path: '/admin/courses', icon: 'C' },
    { label: 'Classrooms', path: '/admin/create-classroom', icon: 'R' },
    { label: 'Assign', path: '/admin/assign-classroom', icon: 'A' },
    { label: 'Enroll', path: '/admin/enroll-student', icon: 'E' }
  ];
}
