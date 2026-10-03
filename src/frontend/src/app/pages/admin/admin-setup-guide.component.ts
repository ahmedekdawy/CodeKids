import { Component, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { LearningApiService } from '../../learning-api.service';
import { SetupGuideComponent, SetupGuideStep } from '../../shared/setup-guide/setup-guide.component';

@Component({
  selector: 'app-admin-setup-guide',
  imports: [SetupGuideComponent],
  template: `<app-setup-guide prefix="admin.guide" [steps]="steps" [counts]="counts()" />`
})
export class AdminSetupGuideComponent {
  private readonly api = inject(LearningApiService);

  readonly steps: SetupGuideStep[] = [
    { id: 'teacher', path: '/admin/teachers' },
    { id: 'course', path: '/admin/courses' },
    { id: 'classroom', path: '/admin/create-classroom' },
    { id: 'assign', path: '/admin/assign-classroom' },
    { id: 'student', path: '/admin/students' },
    { id: 'enroll', path: '/admin/enroll-student' }
  ];

  readonly counts = signal<Record<string, number> | null>(null);

  constructor() {
    forkJoin({
      teachers: this.api.getUsers('Teacher'),
      students: this.api.getUsers('Student'),
      courses: this.api.getCourses(false),
      classrooms: this.api.getClassrooms()
    }).subscribe({
      next: ({ teachers, students, courses, classrooms }) =>
        this.counts.set({
          teacher: teachers.length,
          course: courses.length,
          classroom: classrooms.length,
          assign: classrooms.filter(
            (c) => c.teachers.length > 0 || (c.courses ?? []).some((x) => !!x.teacherId)
          ).length,
          student: students.length,
          enroll: classrooms.reduce((sum, c) => sum + c.students.length, 0)
        }),
      // The guide is optional help; the dashboard shows its own load errors.
      error: () => this.counts.set(null)
    });
  }
}
