import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { TeacherDashboard, TeacherStudentDetail } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-teacher-students',
  imports: [PageFeedbackComponent, IconActionButtonComponent, TranslatePipe],
  templateUrl: './teacher-students.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherStudentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);
  private readonly router = inject(Router);
  readonly dashboard = signal<TeacherDashboard | null>(null);
  readonly detail = signal<TeacherStudentDetail | null>(null);
  readonly error = signal('');
  readonly impersonatingId = signal<string | null>(null);

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

  loginAs(studentId: string): void {
    this.error.set('');
    this.impersonatingId.set(studentId);
    this.auth.impersonateStudentAsTeacher(studentId).subscribe({
      next: () => {
        this.impersonatingId.set(null);
        void this.router.navigateByUrl(this.auth.roleHome());
      },
      error: (err) => {
        this.impersonatingId.set(null);
        this.error.set(this.locale.fromApiError(err, 'teacher.students.loginAsFailed'));
      }
    });
  }
}
