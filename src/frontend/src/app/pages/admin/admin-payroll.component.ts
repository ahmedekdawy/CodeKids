import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { ManagedUser, TeacherPayrollReport } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import {
  GRADE_CODES,
  STAGE_CODES,
  formatGradeLabel,
  formatStageLabel
} from '../../grade.util';
import { downloadElementAsPng } from '../../export-image.util';

@Component({
  selector: 'app-admin-payroll',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './admin-payroll.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminPayrollComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly reportEl = viewChild<ElementRef<HTMLElement>>('reportTable');

  readonly teachers = signal<ManagedUser[]>([]);
  readonly report = signal<TeacherPayrollReport | null>(null);
  readonly message = signal('');
  readonly error = signal('');
  readonly exporting = signal(false);

  readonly grades = GRADE_CODES;
  readonly stages = STAGE_CODES;

  filterFromDate = startOfMonthLocal();
  filterToDate = endOfMonthLocal();
  filterTeacherId = '';
  filterStage: number | '' = '';
  filterGrade: number | '' = '';

  constructor() {
    this.api.getUsers('Teacher').subscribe({
      next: (users) => this.teachers.set(users ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payroll.loadFailed'))
    });
    this.reload();
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
    this.filterFromDate = startOfMonthLocal();
    this.filterToDate = endOfMonthLocal();
    this.filterTeacherId = '';
    this.filterStage = '';
    this.filterGrade = '';
    this.reload();
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
