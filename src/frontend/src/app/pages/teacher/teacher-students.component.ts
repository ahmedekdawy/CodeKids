import { Component, inject, signal } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { TeacherDashboard, TeacherStudentDetail } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-teacher-students',
  imports: [TranslatePipe],
  templateUrl: './teacher-students.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherStudentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly dashboard = signal<TeacherDashboard | null>(null);
  readonly detail = signal<TeacherStudentDetail | null>(null);
  readonly error = signal('');

  constructor() {
    this.api.getTeacherDashboard().subscribe({
      next: (dashboard) => this.dashboard.set(dashboard),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.students.loadFailed'))
    });
  }

  openStudent(studentId: string): void {
    this.error.set('');
    this.api.getTeacherStudentDetail(studentId).subscribe({
      next: (detail) => this.detail.set(detail),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.students.loadDetailFailed'))
    });
  }

  closeDetail(): void {
    this.detail.set(null);
  }
}
