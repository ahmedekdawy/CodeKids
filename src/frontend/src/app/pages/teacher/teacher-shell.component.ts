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

    { labelKey: 'nav.cat.content', path: '/teacher/videos', icon: 'V', categoryKey: 'nav.cat.content' },
    { labelKey: 'nav.teacher.materials', path: '/teacher/materials', icon: 'M', categoryKey: 'nav.cat.content' },
    { labelKey: 'nav.teacher.courseTree', path: '/teacher/course-tree', icon: 'U', categoryKey: 'nav.cat.content' },
    { labelKey: 'nav.teacher.studyPlans', path: '/teacher/study-plans', icon: 'P', categoryKey: 'nav.cat.content' },

    { labelKey: 'nav.teacher.exams', path: '/teacher/exams', icon: 'E', categoryKey: 'nav.cat.assessments' },
    { labelKey: 'nav.teacher.quizzes', path: '/teacher/quizzes', icon: 'Q', categoryKey: 'nav.cat.assessments' },
    { labelKey: 'nav.teacher.assignments', path: '/teacher/assignments', icon: 'A', categoryKey: 'nav.cat.assessments' },
    { labelKey: 'nav.teacher.questionBank', path: '/teacher/question-bank', icon: 'B', categoryKey: 'nav.cat.assessments' },
    { labelKey: 'nav.teacher.review', path: '/teacher/review', icon: 'R', categoryKey: 'nav.cat.assessments' },

    { labelKey: 'nav.teacher.timetable', path: '/teacher/timetable', icon: 'H', categoryKey: 'nav.cat.schedule' },
    { labelKey: 'nav.teacher.appointments', path: '/teacher/appointments', icon: 'K', categoryKey: 'nav.cat.schedule' },
    { labelKey: 'nav.teacher.attendance', path: '/teacher/attendance', icon: 'N', categoryKey: 'nav.cat.schedule' },
    { labelKey: 'nav.teacher.studentAttendance', path: '/teacher/student-attendance', icon: 'A', categoryKey: 'nav.cat.schedule' },

    { labelKey: 'nav.teacher.students', path: '/teacher/students', icon: 'S', categoryKey: 'nav.cat.communication' },
    { labelKey: 'nav.teacher.askedQuestions', path: '/teacher/asked-questions', icon: '?', categoryKey: 'nav.cat.communication' },
    { labelKey: 'nav.teacher.chat', path: '/teacher/chat', icon: 'C', categoryKey: 'nav.cat.communication' },
    { labelKey: 'nav.teacher.weeklyReports', path: '/teacher/weekly-reports', icon: 'W', categoryKey: 'nav.cat.communication' }
  ];
}
