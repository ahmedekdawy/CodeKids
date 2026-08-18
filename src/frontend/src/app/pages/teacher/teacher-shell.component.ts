import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PanelNavItem, PanelShellComponent } from '../../layouts/panel-shell/panel-shell.component';

@Component({
  selector: 'app-teacher-shell',
  imports: [PanelShellComponent, RouterOutlet],
  template: `
    <app-panel-shell
      titleKey="shell.teacher.title"
      subtitleKey="shell.teacher.subtitle"
      [navItems]="navItems">
      <router-outlet />
    </app-panel-shell>
  `
})
export class TeacherShellComponent {
  readonly navItems: PanelNavItem[] = [
    { labelKey: 'nav.teacher.overview', path: '/teacher/overview', icon: 'O' },
    { labelKey: 'nav.teacher.videos', path: '/teacher/videos', icon: 'V' },
    { labelKey: 'nav.teacher.courseTree', path: '/teacher/course-tree', icon: 'U' },
    { labelKey: 'nav.teacher.appointments', path: '/teacher/appointments', icon: 'K' },
    { labelKey: 'nav.teacher.timetable', path: '/teacher/timetable', icon: 'H' },
    { labelKey: 'nav.teacher.attendance', path: '/teacher/attendance', icon: 'N' },
    { labelKey: 'nav.teacher.weeklyReports', path: '/teacher/weekly-reports', icon: 'W' },
    { labelKey: 'nav.teacher.questionBank', path: '/teacher/question-bank', icon: 'B' },
    { labelKey: 'nav.teacher.exams', path: '/teacher/exams', icon: 'E' },
    { labelKey: 'nav.teacher.quizzes', path: '/teacher/quizzes', icon: 'Q' },
    { labelKey: 'nav.teacher.assignments', path: '/teacher/assignments', icon: 'A' },
    { labelKey: 'nav.teacher.review', path: '/teacher/review', icon: 'R' },
    { labelKey: 'nav.teacher.students', path: '/teacher/students', icon: 'S' }
  ];
}
