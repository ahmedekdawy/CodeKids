import { Component, ElementRef, afterNextRender, computed, effect, inject, input, signal, viewChild } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { WeeklyStudyPlan } from '../../models';
import { TranslatePipe } from '../translate.pipe';
import { formatGradeLabel } from '../../grade.util';
import { downloadElementAsPng } from '../../export-image.util';
import { academicYearFromRange, formatStudyPlanRange } from './study-plan.util';

@Component({
  selector: 'app-study-plan-sheet',
  imports: [TranslatePipe],
  templateUrl: './study-plan-sheet.component.html',
  styleUrl: './study-plan-sheet.component.css'
})
export class StudyPlanSheetComponent {
  private readonly locale = inject(LocaleService);
  readonly brand = inject(SiteBrandService);
  readonly isRtl = this.locale.isRtl;

  readonly plan = input.required<WeeklyStudyPlan>();
  readonly showTeacher = input(false);
  readonly exportable = input(true);

  readonly planWrap = viewChild<ElementRef<HTMLElement>>('planWrap');
  readonly exporting = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  readonly sheetTitle = computed(() => {
    this.locale.lang();
    const plan = this.plan();
    return this.locale.t('teacher.studyPlans.sheetTitle', {
      year: academicYearFromRange(plan.fromDate, plan.toDate)
    });
  });

  readonly subjectLine = computed(() => {
    this.locale.lang();
    const plan = this.plan();
    return this.locale.t('teacher.studyPlans.subjectLine', {
      course: plan.courseName || this.locale.t('common.emDash'),
      grade: this.gradeLabel(plan.courseGrade)
    });
  });

  readonly termLine = computed(() => {
    this.locale.lang();
    const plan = this.plan();
    return this.locale.t('teacher.studyPlans.termLine', {
      term: this.termLabel(plan.courseTerm),
      year: academicYearFromRange(plan.fromDate, plan.toDate)
    });
  });

  constructor() {
    afterNextRender(() => this.equalizeWeekCells());
    effect(() => {
      this.plan();
      this.queueEqualize();
    });
  }

  weekLabel(weekNumber: number): string {
    const key = `teacher.studyPlans.week.${weekNumber}`;
    const translated = this.locale.t(key);
    return translated === key ? this.locale.t('teacher.studyPlans.weekN', { n: weekNumber }) : translated;
  }

  formatRange(from: string, to: string): string {
    return formatStudyPlanRange(from, to);
  }

  async exportAsImage(): Promise<void> {
    const wrap = this.planWrap()?.nativeElement;
    if (!wrap || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.message.set('');
    this.equalizeWeekCells();
    try {
      await downloadElementAsPng(wrap, `${this.exportFileName()}.png`, { backgroundColor: '#ffffff' });
      this.message.set(this.locale.t('teacher.studyPlans.exported'));
    } catch {
      this.error.set(this.locale.t('teacher.studyPlans.exportFailed'));
    } finally {
      this.exporting.set(false);
    }
  }

  private gradeLabel(grade?: number | null): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  private termLabel(term?: string | null): string {
    if (term === 'FirstTerm') return this.locale.t('term.first');
    if (term === 'SecondTerm') return this.locale.t('term.second');
    if (term === 'FullYear') return this.locale.t('term.full');
    return this.locale.t('common.emDash');
  }

  private queueEqualize(): void {
    requestAnimationFrame(() => requestAnimationFrame(() => this.equalizeWeekCells()));
  }

  private equalizeWeekCells(): void {
    const grid = this.planWrap()?.nativeElement?.querySelector('.study-plan-grid') as HTMLElement | null;
    if (!grid) return;
    const cells = [...grid.querySelectorAll<HTMLElement>('.study-week')];
    for (const cell of cells) {
      cell.style.height = '';
      cell.style.minHeight = '';
    }
    const max = Math.max(260, ...cells.map((cell) => cell.scrollHeight));
    for (const cell of cells) {
      cell.style.minHeight = `${max}px`;
      cell.style.height = `${max}px`;
    }
  }

  private exportFileName(): string {
    const plan = this.plan();
    const slug = (plan.courseName || 'study-plan')
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9\u0600-\u06ff]+/gi, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, 40);
    return `study-plan-${slug || 'course'}-${plan.fromDate}-${plan.toDate}`;
  }
}
