import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { ManagedUser, StudentWeeklyReport } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { GRADE_CODES, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-admin-weekly-reports',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, TranslatePipe],
  templateUrl: './admin-weekly-reports.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminWeeklyReportsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly teachers = signal<ManagedUser[]>([]);
  readonly rows = signal<StudentWeeklyReport[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly filterTeacherId = signal('');
  readonly filterGrade = signal<number | ''>('');
  readonly filterFromDate = signal(startOfMonthLocal());
  readonly filterToDate = signal(endOfMonthLocal());

  readonly teacherOptions = computed(() =>
    this.teachers()
      .slice()
      .sort((a, b) => a.displayName.localeCompare(b.displayName))
      .map((teacher) => ({ value: teacher.id, label: teacher.displayName }))
  );

  readonly gradeOptions = computed(() => {
    this.locale.lang();
    return GRADE_CODES.map((g) => ({
      value: g,
      label: formatGradeLabel((k, p) => this.locale.t(k, p), g)
    }));
  });

  constructor() {
    this.api.getUsers('Teacher').subscribe({
      next: (users) => this.teachers.set(users ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.weeklyReports.loadFailed'))
    });
    this.loadRows();
  }

  gradeLabel(grade?: number | null): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  interactionLabel(value: string): string {
    if (!value) return this.locale.t('common.emDash');
    const key = `teacher.weeklyReports.interaction.${value}`;
    const translated = this.locale.t(key);
    return translated === key ? value : translated;
  }

  openCameraLabel(value: boolean | null | undefined): string {
    if (value === true) return this.locale.t('common.true');
    if (value === false) return this.locale.t('common.false');
    return this.locale.t('common.emDash');
  }

  loadRows(): void {
    this.message.set('');
    this.error.set('');
    this.api
      .listAdminWeeklyReports({
        teacherId: this.filterTeacherId() || undefined,
        grade: this.filterGrade() === '' ? undefined : Number(this.filterGrade()),
        fromDate: this.filterFromDate() || undefined,
        toDate: this.filterToDate() || undefined
      })
      .subscribe({
        next: (rows) => this.rows.set(rows ?? []),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.weeklyReports.loadFailed'))
      });
  }

  setFilterTeacher(teacherId: string): void {
    this.filterTeacherId.set(teacherId);
    this.loadRows();
  }

  setFilterGrade(grade: number | ''): void {
    this.filterGrade.set(grade);
    this.loadRows();
  }

  resetFilters(): void {
    this.filterTeacherId.set('');
    this.filterGrade.set('');
    this.filterFromDate.set(startOfMonthLocal());
    this.filterToDate.set(endOfMonthLocal());
    this.loadRows();
  }
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
