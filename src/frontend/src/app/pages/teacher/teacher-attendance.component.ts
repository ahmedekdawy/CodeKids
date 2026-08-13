import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, Course, TeacherSessionAttendance } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { GRADE_CODES, formatCourseLabel, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';

@Component({
  selector: 'app-teacher-attendance',
  imports: [SearchableSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './teacher-attendance.component.html',
  styleUrl: '../admin/admin-panel.css'
})
export class TeacherAttendanceComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly rows = signal<TeacherSessionAttendance[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly formGrade = signal<number | ''>('');
  readonly filterGrade = signal<number | ''>('');
  readonly filterFromDate = signal(startOfMonthLocal());
  readonly filterToDate = signal(endOfMonthLocal());

  readonly grades = GRADE_CODES;
  courseId = '';
  sessionDate = todayLocal();

  readonly availableCourses = computed(() => {
    const courseIds = new Set(
      this.classrooms()
        .flatMap((room) => room.courses ?? [])
        .map((link) => link.courseId)
    );
    const grade = this.formGrade();
    return this.courses()
      .filter((course) => courseIds.has(course.id))
      .filter((course) => grade === '' || course.grade === grade)
      .slice()
      .sort((a, b) => {
        const ga = a.grade ?? 999;
        const gb = b.grade ?? 999;
        if (ga !== gb) return ga - gb;
        return a.title.localeCompare(b.title);
      });
  });

  constructor() {
    this.api.getCourses().subscribe({
      next: (courses) => this.courses.set(courses ?? []),
      error: () => undefined
    });
    this.api.getClassrooms().subscribe({
      next: (rooms) => this.classrooms.set(rooms ?? []),
      error: () => undefined
    });
    this.reload();
  }

  gradeLabel(grade?: number | null): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade);
  }

  onGradeChange(grade: number | ''): void {
    this.formGrade.set(grade);
    const stillValid = this.availableCourses().some((c) => c.id === this.courseId);
    if (!stillValid) this.courseId = '';
  }

  reload(): void {
    this.clearStatus();
    this.api
      .getMySessionAttendance({
        grade: this.filterGrade() === '' ? undefined : Number(this.filterGrade()),
        fromDate: this.filterFromDate() || undefined,
        toDate: this.filterToDate() || undefined
      })
      .subscribe({
        next: (rows) => this.rows.set(rows ?? []),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.attendance.loadFailed'))
      });
  }

  resetDateFilters(): void {
    this.filterFromDate.set(startOfMonthLocal());
    this.filterToDate.set(endOfMonthLocal());
    this.reload();
  }

  create(): void {
    this.clearStatus();
    if (!this.courseId || !this.sessionDate) {
      this.error.set(this.locale.t('teacher.attendance.requiredFields'));
      return;
    }
    this.api
      .createMySessionAttendance({
        courseId: this.courseId,
        sessionDate: this.sessionDate
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('teacher.attendance.created'));
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.attendance.createFailed'))
      });
  }

  remove(row: TeacherSessionAttendance): void {
    if (!confirm(this.locale.t('teacher.attendance.confirmDelete', { label: row.label }))) return;
    this.clearStatus();
    this.api.deleteMySessionAttendance(row.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('teacher.attendance.deleted'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.attendance.deleteFailed'))
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}

function todayLocal(): string {
  const d = new Date();
  return toLocalDateString(d);
}

function startOfMonthLocal(): string {
  const d = new Date();
  return toLocalDateString(new Date(d.getFullYear(), d.getMonth(), 1));
}

function endOfMonthLocal(): string {
  const d = new Date();
  return toLocalDateString(new Date(d.getFullYear(), d.getMonth() + 1, 0));
}

function toLocalDateString(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}
