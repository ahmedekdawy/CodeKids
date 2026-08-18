import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { SaveWeeklyReportEntry, StudentWeeklyReport, StudentWeeklyReportGridRow } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { GRADE_CODES, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

type EditableRow = StudentWeeklyReportGridRow & { dirty?: boolean };

@Component({
  selector: 'app-teacher-weekly-reports',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, TranslatePipe],
  templateUrl: './teacher-weekly-reports.component.html',
  styleUrl: '../admin/admin-panel.css'
})
export class TeacherWeeklyReportsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly grades = GRADE_CODES;
  readonly interactionOptions = ['Excellent', 'Good', 'Fair', 'Poor'] as const;

  readonly gridRows = signal<EditableRow[]>([]);
  readonly historyRows = signal<StudentWeeklyReport[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly saving = signal(false);
  readonly filterGrade = signal<number | ''>('');
  readonly filterFromDate = signal(startOfMonthLocal());
  readonly filterToDate = signal(endOfMonthLocal());
  weekStartDate = startOfWeekLocal(new Date());

  constructor() {
    this.loadGrid();
    this.loadHistory();
  }

  gradeLabel(grade?: number | null): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  interactionLabel(value: string): string {
    if (!value) return this.locale.t('common.select');
    const key = `teacher.weeklyReports.interaction.${value}`;
    const translated = this.locale.t(key);
    return translated === key ? value : translated;
  }

  openCameraLabel(value: boolean | null | undefined): string {
    if (value === true) return this.locale.t('common.true');
    if (value === false) return this.locale.t('common.false');
    return this.locale.t('common.select');
  }

  onWeekChange(value: string): void {
    this.weekStartDate = startOfWeekLocal(new Date(value + 'T00:00:00'));
    this.loadGrid();
  }

  onGradeChange(grade: number | ''): void {
    this.filterGrade.set(grade);
    this.loadGrid();
  }

  loadGrid(): void {
    this.clearStatus();
    this.api
      .getWeeklyReportGrid({
        weekStart: this.weekStartDate,
        grade: this.filterGrade() === '' ? undefined : Number(this.filterGrade())
      })
      .subscribe({
        next: (rows) => this.gridRows.set((rows ?? []).map((row) => ({ ...row, dirty: false }))),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.weeklyReports.loadFailed'))
      });
  }

  loadHistory(keepFeedback = false): void {
    if (!keepFeedback) {
      this.clearStatus();
    }
    this.api
      .listWeeklyReports({
        grade: this.filterGrade() === '' ? undefined : Number(this.filterGrade()),
        fromDate: this.filterFromDate() || undefined,
        toDate: this.filterToDate() || undefined
      })
      .subscribe({
        next: (rows) => this.historyRows.set(rows ?? []),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.weeklyReports.historyFailed'))
      });
  }

  resetDateFilters(): void {
    this.filterFromDate.set(startOfMonthLocal());
    this.filterToDate.set(endOfMonthLocal());
    this.loadHistory();
  }

  markDirty(row: EditableRow): void {
    row.dirty = true;
  }

  save(): void {
    this.clearStatus();
    const entries: SaveWeeklyReportEntry[] = this.gridRows().map((row) => ({
      studentId: row.studentId,
      performancePercent: row.performancePercent ?? null,
      attendancePercent: row.attendancePercent ?? null,
      homeworkPercent: row.homeworkPercent ?? null,
      interactionDuringSession: row.interactionDuringSession ?? '',
      openCamera: row.openCamera ?? null
    }));

    this.saving.set(true);
    this.api
      .saveWeeklyReports({
        weekStartDate: this.weekStartDate,
        entries
      })
      .subscribe({
        next: (rows) => {
          this.gridRows.set((rows ?? []).map((row) => ({ ...row, dirty: false })));
          this.saving.set(false);
          this.loadHistory(true);
          this.message.set(this.locale.t('teacher.weeklyReports.saved'));
        },
        error: (err) => {
          this.error.set(this.locale.fromApiError(err, 'teacher.weeklyReports.saveFailed'));
          this.saving.set(false);
        }
      });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}

function startOfWeekLocal(d: Date): string {
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  const monday = new Date(d);
  monday.setDate(d.getDate() + diff);
  return toLocalDateString(monday);
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
