import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ClassroomDiagnosis, TeacherDashboard } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatGradeLabel } from '../../grade.util';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

type GradeStudentGroup = {
  grade: number | null;
  gradeLabel: string;
  courseTitles: string[];
  students: { studentId: string; displayName: string }[];
  zoomLinks: { classroomName: string; name: string; url: string }[];
};

@Component({
  selector: 'app-teacher-overview',
  imports: [FormsModule, TranslatePipe, PageFeedbackComponent],
  templateUrl: './teacher-overview.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherOverviewComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly dashboard = signal<TeacherDashboard | null>(null);
  readonly classrooms = signal<Classroom[]>([]);
  readonly diagnosis = signal<ClassroomDiagnosis | null>(null);
  readonly savingAccount = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  email = this.auth.user()?.email ?? '';
  mobilePhone = this.auth.user()?.mobilePhone ?? '';
  password = '';
  passwordConfirm = '';

  readonly studentsByGrade = computed<GradeStudentGroup[]>(() => {
    this.locale.lang();
    const teacherId = this.dashboard()?.teacherId;
    if (!teacherId) return [];

    const byGrade = new Map<
      string,
      {
        grade: number | null;
        courses: Set<string>;
        students: Map<string, string>;
        zoomLinks: { classroomName: string; name: string; url: string }[];
      }
    >();

    for (const room of this.classrooms()) {
      const myCourses = (room.courses ?? []).filter((c) => c.teacherId === teacherId);
      const courseEntries =
        myCourses.length > 0
          ? myCourses
          : room.courseId
            ? [
                {
                  courseTitle: room.courseTitle || '',
                  courseGrade: room.courseGrade ?? room.grade ?? null
                }
              ]
            : [];

      if (!courseEntries.length) continue;

      for (const course of courseEntries) {
        const grade = course.courseGrade ?? room.grade ?? null;
        const key = grade == null ? 'all' : String(grade);
        let group = byGrade.get(key);
        if (!group) {
          group = { grade, courses: new Set(), students: new Map(), zoomLinks: [] };
          byGrade.set(key, group);
        }
        if (course.courseTitle) group.courses.add(course.courseTitle);
        for (const link of room.zoomLinks ?? []) {
          const name = (link.name || '').trim();
          const url = (link.url || '').trim();
          if (!name || !url) continue;
          if (!group.zoomLinks.some((existing) => existing.classroomName === room.name && existing.url === url)) {
            group.zoomLinks.push({ classroomName: room.name, name, url });
          }
        }
        for (const student of room.students ?? []) {
          group.students.set(student.studentId, student.displayName);
        }
      }
    }

    return [...byGrade.values()]
      .map((group) => ({
        grade: group.grade,
        gradeLabel: formatGradeLabel((k, p) => this.locale.t(k, p), group.grade),
        courseTitles: [...group.courses].sort((a, b) => a.localeCompare(b)),
        students: [...group.students.entries()]
          .map(([studentId, displayName]) => ({ studentId, displayName }))
          .sort((a, b) => a.displayName.localeCompare(b.displayName)),
        zoomLinks: [...group.zoomLinks].sort((a, b) => a.name.localeCompare(b.name))
      }))
      .sort((a, b) => {
        if (a.grade == null && b.grade == null) return 0;
        if (a.grade == null) return 1;
        if (b.grade == null) return -1;
        return a.grade - b.grade;
      });
  });

  constructor() {
    this.api.getTeacherDashboard().subscribe((dashboard) => this.dashboard.set(dashboard));
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (classrooms[0]) this.loadDiagnosis(classrooms[0].id);
    });
  }

  saveAccount(): void {
    this.error.set('');
    this.message.set('');
    if (!this.email.trim() && !this.mobilePhone.trim()) {
      this.error.set(this.locale.t('admin.users.emailOrMobileRequired'));
      return;
    }
    if (this.password || this.passwordConfirm) {
      if (this.password !== this.passwordConfirm) {
        this.error.set(this.locale.t('auth.reset.mismatch'));
        return;
      }
      if (this.password.trim().length < 6) {
        this.error.set(this.locale.t('api.errors.auth.passwordTooShort'));
        return;
      }
    }

    this.savingAccount.set(true);
    this.auth
      .updateAccount({
        email: this.email.trim() || null,
        mobilePhone: this.mobilePhone.trim() || null,
        password: this.password.trim() || null
      })
      .subscribe({
        next: (user) => {
          this.savingAccount.set(false);
          this.email = user.email ?? '';
          this.mobilePhone = user.mobilePhone ?? '';
          this.password = '';
          this.passwordConfirm = '';
          this.message.set(this.locale.t('teacher.account.saved'));
        },
        error: (err) => {
          this.savingAccount.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.account.saveFailed'));
        }
      });
  }

  loadDiagnosis(classroomId: string): void {
    this.api.getClassroomDiagnosis(classroomId).subscribe({
      next: (diagnosis) => this.diagnosis.set(diagnosis),
      error: () => this.diagnosis.set(null)
    });
  }
}
