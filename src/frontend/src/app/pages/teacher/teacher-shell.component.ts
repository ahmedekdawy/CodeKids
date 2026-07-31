import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PanelNavItem, PanelShellComponent } from '../../layouts/panel-shell/panel-shell.component';

@Component({
  selector: 'app-teacher-shell',
  imports: [PanelShellComponent, RouterOutlet],
  template: `
    <app-panel-shell
      title="Teacher"
      subtitle="Classroom workspace"
      [navItems]="navItems">
      <router-outlet />
    </app-panel-shell>
  `
})
export class TeacherShellComponent {
  readonly navItems: PanelNavItem[] = [
    { label: 'Overview', path: '/teacher/overview', icon: 'O' },
    { label: 'Zoom', path: '/teacher/zoom', icon: 'Z' },
    { label: 'Quizzes', path: '/teacher/quizzes', icon: 'Q' },
    { label: 'Assignments', path: '/teacher/assignments', icon: 'A' },
    { label: 'Review', path: '/teacher/review', icon: 'R' },
    { label: 'Students', path: '/teacher/students', icon: 'S' }
  ];
}
