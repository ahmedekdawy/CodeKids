import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { ManagedUser, TeacherPayrollAdjustment, TeacherPayrollReport } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import {
  GRADE_CODES,
  STAGE_CODES,
  formatGradeLabel,
  formatStageLabel
} from '../../grade.util';
import { downloadElementAsPng } from '../../export-image.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';

@Component({
  selector: 'app-admin-payroll',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    FormsModule,
    TranslatePipe,
    IconActionButtonComponent
  ],
  templateUrl: './admin-payroll.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminPayrollComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly reportEl = viewChild<ElementRef<HTMLElement>>('reportTable');

  readonly teachers = signal<ManagedUser[]>([]);
  readonly report = signal<TeacherPayrollReport | null>(null);
  readonly adjustments = signal<TeacherPayrollAdjustment[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly exporting = signal(false);

  readonly grades = GRADE_CODES;
  readonly stages = STAGE_CODES;
  readonly months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
  readonly years = yearOptions();

  filterYear = new Date().getFullYear();
  filterMonth = new Date().getMonth() + 1;
  filterFromDate = startOfMonthLocal();
  filterToDate = endOfMonthLocal();
  filterTeacherId = '';
  filterStage: number | '' = '';
  filterGrade: number | '' = '';

  manualTeacherId = '';
  manualAmount: number | null = null;
  manualYear = new Date().getFullYear();
  manualMonth = new Date().getMonth() + 1;
  manualNotes = '';

  manualFilterYear = new Date().getFullYear();
  manualFilterMonth = new Date().getMonth() + 1;
  manualFilterTeacherId = '';

  constructor() {
    this.api.getUsers('Teacher').subscribe({
      next: (users) => this.teachers.set(users ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payroll.loadFailed'))
    });
    this.reload();
    this.loadAdjustments();
  }

  monthLabel(month: number): string {
    return this.locale.t(`admin.payments.month${month}`);
  }

  adjustmentMonth(row: TeacherPayrollAdjustment): number {
    return parseMonthYear(row.adjustmentDate).month;
  }

  adjustmentYear(row: TeacherPayrollAdjustment): number {
    return parseMonthYear(row.adjustmentDate).year;
  }

  gradeLabel(grade: number): string {
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  stageLabel(stage: number): string {
    return formatStageLabel((k, p) => this.locale.t(k, p), stage);
  }

  moneyLabel(value: number): string {
    return value.toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  }

  resetFilters(): void {
    const now = new Date();
    this.filterYear = now.getFullYear();
    this.filterMonth = now.getMonth() + 1;
    this.applyMonthYearFilter();
    this.filterTeacherId = '';
    this.filterStage = '';
    this.filterGrade = '';
    this.reload();
  }

  onFilterMonthYearChange(): void {
    this.applyMonthYearFilter();
    this.reload();
  }

  onDateRangeChange(): void {
    if (this.filterFromDate) {
      const parsed = parseMonthYear(this.filterFromDate);
      this.filterYear = parsed.year;
      this.filterMonth = parsed.month;
    }
    this.reload();
  }

  private applyMonthYearFilter(): void {
    const range = monthRange(this.filterYear, this.filterMonth);
    this.filterFromDate = range.from;
    this.filterToDate = range.to;
  }

  reload(): void {
    this.clearStatus();
    if (!this.filterFromDate || !this.filterToDate) {
      this.error.set(this.locale.t('admin.payroll.dateRequired'));
      return;
    }

    this.api
      .getPayrollReport({
        fromDate: this.filterFromDate,
        toDate: this.filterToDate,
        teacherId: this.filterTeacherId || undefined,
        stage: this.filterStage === '' ? undefined : Number(this.filterStage),
        grade: this.filterGrade === '' ? undefined : Number(this.filterGrade)
      })
      .subscribe({
        next: (report) => this.report.set(report),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payroll.loadFailed'))
      });
  }

  onManualFilterChange(): void {
    this.loadAdjustments();
  }

  resetManualFilters(): void {
    const now = new Date();
    this.manualFilterYear = now.getFullYear();
    this.manualFilterMonth = now.getMonth() + 1;
    this.manualFilterTeacherId = '';
    this.loadAdjustments();
  }

  loadAdjustments(): void {
    const range = monthRange(this.manualFilterYear, this.manualFilterMonth);
    this.api
      .getPayrollAdjustments({
        fromDate: range.from,
        toDate: range.to,
        teacherId: this.manualFilterTeacherId || undefined
      })
      .subscribe({
        next: (rows) => this.adjustments.set(rows ?? []),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payroll.adjustmentsLoadFailed'))
      });
  }

  createManualAdjustment(): void {
    this.clearStatus();
    const amount = Number(this.manualAmount);
    if (!this.manualTeacherId || !this.manualYear || !this.manualMonth || !Number.isFinite(amount) || amount === 0) {
      this.error.set(this.locale.t('admin.payroll.manualRequired'));
      return;
    }

    const adjustmentDate = monthRange(this.manualYear, this.manualMonth).from;

    this.api
      .createPayrollAdjustment({
        teacherId: this.manualTeacherId,
        amount,
        adjustmentDate,
        notes: this.manualNotes.trim()
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.payroll.manualCreated'));
          this.manualAmount = null;
          this.manualNotes = '';
          this.reload();
          this.loadAdjustments();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payroll.manualCreateFailed'))
      });
  }

  removeAdjustment(row: TeacherPayrollAdjustment): void {
    if (!confirm(this.locale.t('admin.payroll.confirmDeleteManual', { label: row.teacherName }))) return;
    this.clearStatus();
    this.api.deletePayrollAdjustment(row.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.payroll.manualDeleted'));
        this.reload();
        this.loadAdjustments();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payroll.manualDeleteFailed'))
    });
  }

  async exportImage(): Promise<void> {
    const el = this.reportEl()?.nativeElement;
    if (!el) return;
    this.clearStatus();
    this.exporting.set(true);
    try {
      const from = this.filterFromDate;
      const to = this.filterToDate;
      await downloadElementAsPng(el, `payroll-${from}-to-${to}`);
      this.message.set(this.locale.t('admin.payroll.exported'));
    } catch {
      this.error.set(this.locale.t('admin.payroll.exportFailed'));
    } finally {
      this.exporting.set(false);
    }
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}

function yearOptions(): number[] {
  const current = new Date().getFullYear();
  return Array.from({ length: 6 }, (_, i) => current - 2 + i);
}

function monthRange(year: number, month: number): { from: string; to: string } {
  return {
    from: toLocalDateString(new Date(year, month - 1, 1)),
    to: toLocalDateString(new Date(year, month, 0))
  };
}

function parseMonthYear(dateStr: string): { month: number; year: number } {
  const d = new Date(`${dateStr}T00:00:00`);
  return { month: d.getMonth() + 1, year: d.getFullYear() };
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
