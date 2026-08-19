import { Component, ElementRef, afterNextRender, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { SiteBrandService } from '../../site-brand.service';
import { Course, SaveWeeklyStudyPlanWeek, WeeklyStudyPlan } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatCourseLabel, formatGradeLabel } from '../../grade.util';
import { downloadElementAsPng } from '../../export-image.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { normalizeStudyPlan, normalizeStudyPlans } from '../../shared/study-plan-sheet/study-plan.util';

type EditableTopic = {
  title: string;
  highlight: boolean;
};

type EditableWeek = {
  weekNumber: number;
  fromDate: string;
  toDate: string;
  topics: EditableTopic[];
};

const MAX_WEEKS = 20;
const DEFAULT_WEEKS = 18;

@Component({
  selector: 'app-teacher-study-plans',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    FormsModule,
    TranslatePipe,
    IconActionButtonComponent
  ],
  templateUrl: './teacher-study-plans.component.html',
  styleUrls: ['../admin/admin-panel.css', './teacher-study-plans.component.css']
})
export class TeacherStudyPlansComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly brand = inject(SiteBrandService);
  readonly isRtl = this.locale.isRtl;

  readonly planWrap = viewChild<ElementRef<HTMLElement>>('planWrap');

  readonly courses = signal<Course[]>([]);
  readonly plans = signal<WeeklyStudyPlan[]>([]);
  readonly weeks = signal<EditableWeek[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly saving = signal(false);
  readonly exporting = signal(false);

  readonly filterCourseId = signal('');
  readonly filterFromDate = signal(defaultFromDate());
  readonly filterToDate = signal(defaultToDate());
  readonly editorTick = signal(0);

  editingId: string | null = null;
  courseId = '';
  fromDate = defaultFromDate();
  toDate = defaultToDate();
  notes = '';
  private editorLoadSeq = 0;

  readonly courseOptions = computed(() =>
    this.courses()
      .slice()
      .sort((a, b) => {
        const ga = a.grade ?? 999;
        const gb = b.grade ?? 999;
        if (ga !== gb) return ga - gb;
        return a.title.localeCompare(b.title);
      })
      .map((course) => ({ value: course.id, label: this.courseLabel(course) }))
  );

  readonly selectedCourse = computed(() => {
    this.editorTick();
    return this.courses().find((c) => c.id === this.courseId) ?? null;
  });

  readonly sheetTitle = computed(() => {
    this.locale.lang();
    return this.locale.t('teacher.studyPlans.sheetTitle', { year: academicYear(this.fromDate, this.toDate) });
  });

  readonly subjectLine = computed(() => {
    this.locale.lang();
    this.editorTick();
    const course = this.selectedCourse();
    return this.locale.t('teacher.studyPlans.subjectLine', {
      course: course ? course.title : this.locale.t('common.emDash'),
      grade: this.gradeLabel(course?.grade)
    });
  });

  readonly termLine = computed(() => {
    this.locale.lang();
    this.editorTick();
    const course = this.selectedCourse();
    return this.locale.t('teacher.studyPlans.termLine', {
      term: this.termLabel(course?.term),
      year: academicYear(this.fromDate, this.toDate)
    });
  });

  constructor() {
    afterNextRender(() => this.equalizeWeekCells());
    this.rebuildWeeks();
    this.api.getCourses().subscribe({
      next: (courses) => {
        this.courses.set(courses ?? []);
        if (!this.courseId && this.courses().length === 1) {
          this.courseId = this.courses()[0].id;
          this.tryLoadMatchingPlan();
        }
        this.editorTick.update((n) => n + 1);
      },
      error: () => undefined
    });
    this.loadPlans();
  }

  gradeLabel(grade?: number | null): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade);
  }

  planCourseLabel(plan: WeeklyStudyPlan): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), plan.courseName, plan.courseGrade);
  }

  termLabel(term?: string | null): string {
    if (term === 'FirstTerm') return this.locale.t('term.first');
    if (term === 'SecondTerm') return this.locale.t('term.second');
    if (term === 'FullYear') return this.locale.t('term.full');
    return this.locale.t('common.emDash');
  }

  weekLabel(weekNumber: number): string {
    const key = `teacher.studyPlans.week.${weekNumber}`;
    const translated = this.locale.t(key);
    return translated === key ? this.locale.t('teacher.studyPlans.weekN', { n: weekNumber }) : translated;
  }

  formatRange(from: string, to: string): string {
    return `${formatShortDate(from)} – ${formatShortDate(to)}`;
  }

  onEditorCourseChange(courseId: string): void {
    this.courseId = courseId;
    this.tryLoadMatchingPlan();
  }

  onFromDateChange(value: string): void {
    this.fromDate = value;
    this.rebuildWeeks();
    this.tryLoadMatchingPlan();
  }

  onToDateChange(value: string): void {
    this.toDate = value;
    this.rebuildWeeks();
  }

  thisTerm(): void {
    this.fromDate = defaultFromDate();
    this.toDate = defaultToDate();
    this.rebuildWeeks();
    this.tryLoadMatchingPlan();
  }

  newPlan(): void {
    this.editingId = null;
    this.notes = '';
    this.rebuildWeeks([]);
    this.editorTick.update((n) => n + 1);
    this.clearStatus();
  }

  addTopic(week: EditableWeek): void {
    week.topics.push({ title: '', highlight: false });
    this.queueEqualize();
  }

  removeTopic(week: EditableWeek, index: number): void {
    week.topics.splice(index, 1);
    if (week.topics.length === 0) this.addTopic(week);
    this.queueEqualize();
  }

  onTopicInput(event: Event): void {
    const area = event.target as HTMLTextAreaElement | null;
    if (area) autosizeTextarea(area);
    this.queueEqualize();
  }

  loadPlans(): void {
    this.clearStatus();
    this.api
      .listStudyPlans({
        courseId: this.filterCourseId() || undefined,
        fromDate: this.filterFromDate() || undefined,
        toDate: this.filterToDate() || undefined
      })
      .subscribe({
        next: (rows) => this.plans.set(normalizeStudyPlans(rows)),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.studyPlans.loadFailed'))
      });
  }

  resetFilters(): void {
    this.filterCourseId.set('');
    this.filterFromDate.set(defaultFromDate());
    this.filterToDate.set(defaultToDate());
    this.loadPlans();
  }

  editPlan(plan: WeeklyStudyPlan): void {
    this.applyPlan(plan);
    this.clearStatus();
  }

  save(): void {
    this.clearStatus();
    if (!this.courseId || !this.fromDate || !this.toDate) {
      this.error.set(this.locale.t('teacher.studyPlans.required'));
      return;
    }
    if (buildSchoolWeeks(this.fromDate, this.toDate).length > MAX_WEEKS) {
      this.error.set(this.locale.t('teacher.studyPlans.rangeTooLong'));
      return;
    }

    const weeks: SaveWeeklyStudyPlanWeek[] = this.weeks().map((week) => ({
      weekNumber: week.weekNumber,
      fromDate: week.fromDate,
      toDate: week.toDate,
      topics: week.topics
        .map((topic) => ({ title: topic.title.trim(), highlight: topic.highlight }))
        .filter((topic) => topic.title)
    }));

    this.saving.set(true);
    this.api
      .saveStudyPlan({
        id: this.editingId,
        courseId: this.courseId,
        fromDate: this.fromDate,
        toDate: this.toDate,
        notes: this.notes,
        weeks
      })
      .subscribe({
        next: (plan) => {
          this.applyPlan(normalizeStudyPlan(plan));
          this.saving.set(false);
          this.loadPlans();
          this.message.set(this.locale.t('teacher.studyPlans.saved'));
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.studyPlans.saveFailed'));
        }
      });
  }

  remove(plan: WeeklyStudyPlan): void {
    const label = `${this.planCourseLabel(plan)} (${plan.fromDate} – ${plan.toDate})`;
    if (!confirm(this.locale.t('teacher.studyPlans.confirmDelete', { label }))) return;
    this.clearStatus();
    this.api.deleteStudyPlan(plan.id).subscribe({
      next: () => {
        if (this.editingId === plan.id) this.newPlan();
        this.message.set(this.locale.t('teacher.studyPlans.deleted'));
        this.loadPlans();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.studyPlans.deleteFailed'))
    });
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

  private tryLoadMatchingPlan(): void {
    this.editorTick.update((n) => n + 1);
    if (!this.courseId || !this.fromDate) {
      this.editingId = null;
      return;
    }

    const seq = ++this.editorLoadSeq;
    this.api
      .listStudyPlans({
        courseId: this.courseId,
        fromDate: this.fromDate,
        toDate: this.fromDate
      })
      .subscribe({
        next: (rows) => {
          if (seq !== this.editorLoadSeq) return;
          const match = normalizeStudyPlans(rows).find(
            (plan) => plan.courseId === this.courseId && plan.fromDate === this.fromDate
          );
          if (match) this.applyPlan(match);
          else this.editingId = null;
        },
        error: () => undefined
      });
  }

  private applyPlan(plan: WeeklyStudyPlan): void {
    this.editingId = plan.id;
    this.courseId = plan.courseId;
    this.fromDate = plan.fromDate;
    this.toDate = plan.toDate;
    this.notes = plan.notes ?? '';
    this.rebuildWeeks(plan.weeks);
    this.editorTick.update((n) => n + 1);
  }

  private rebuildWeeks(existing?: { weekNumber: number; fromDate: string; toDate: string; topics: EditableTopic[] }[]): void {
    const previous = new Map((existing ?? this.weeks()).map((week) => [week.weekNumber, week]));
    this.weeks.set(
      buildSchoolWeeks(this.fromDate, this.toDate).map((slot) => {
        const found = previous.get(slot.weekNumber);
        const topics = (found?.topics ?? []).map((topic) => ({ ...topic }));
        return {
          weekNumber: slot.weekNumber,
          fromDate: found?.fromDate || slot.fromDate,
          toDate: found?.toDate || slot.toDate,
          topics: topics.length ? topics : [{ title: '', highlight: false }]
        };
      })
    );
    this.queueEqualize();
  }

  private queueEqualize(): void {
    requestAnimationFrame(() => requestAnimationFrame(() => this.equalizeWeekCells()));
  }

  private equalizeWeekCells(): void {
    const grid = this.planWrap()?.nativeElement?.querySelector('.study-plan-grid') as HTMLElement | null;
    if (!grid) return;
    const cells = [...grid.querySelectorAll<HTMLElement>('.study-week')];
    const areas = [...grid.querySelectorAll<HTMLTextAreaElement>('.study-topic textarea')];
    for (const area of areas) autosizeTextarea(area);
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
    const course = this.selectedCourse();
    const slug = (course?.title || 'study-plan')
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9\u0600-\u06ff]+/gi, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, 40);
    return `study-plan-${slug || 'course'}-${this.fromDate}-${this.toDate}`;
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}

function autosizeTextarea(area: HTMLTextAreaElement): void {
  area.style.height = 'auto';
  area.style.height = `${Math.max(area.scrollHeight, 52)}px`;
}

function defaultFromDate(): string {
  return startOfSchoolWeek(new Date());
}

function defaultToDate(): string {
  const start = parseLocalDate(defaultFromDate());
  if (!start) return toLocalDateString(new Date());
  start.setDate(start.getDate() + (DEFAULT_WEEKS - 1) * 7 + 4);
  return toLocalDateString(start);
}

function startOfSchoolWeek(d: Date): string {
  const sunday = new Date(d);
  sunday.setDate(d.getDate() - d.getDay());
  return toLocalDateString(sunday);
}

function buildSchoolWeeks(from: string, to: string): { weekNumber: number; fromDate: string; toDate: string }[] {
  const start = parseLocalDate(from);
  const end = parseLocalDate(to);
  if (!start || !end || end < start) return [];
  const sunday = new Date(start);
  sunday.setDate(start.getDate() - start.getDay());
  const weeks: { weekNumber: number; fromDate: string; toDate: string }[] = [];
  const cursor = new Date(sunday);
  while (cursor <= end && weeks.length < MAX_WEEKS) {
    const weekFrom = new Date(Math.max(cursor.getTime(), start.getTime()));
    const thursday = new Date(cursor);
    thursday.setDate(cursor.getDate() + 4);
    const weekTo = new Date(Math.min(thursday.getTime(), end.getTime()));
    if (weekFrom <= weekTo) {
      weeks.push({
        weekNumber: weeks.length + 1,
        fromDate: toLocalDateString(weekFrom),
        toDate: toLocalDateString(weekTo)
      });
    }
    cursor.setDate(cursor.getDate() + 7);
  }
  return weeks;
}

function academicYear(from: string, to: string): string {
  const start = parseLocalDate(from);
  const end = parseLocalDate(to) ?? start;
  if (!start) return String(new Date().getFullYear());
  const y1 = start.getFullYear();
  const y2 = end?.getFullYear() ?? y1;
  return y1 === y2 ? String(y1) : `${y1} - ${y2}`;
}

function formatShortDate(value: string): string {
  const date = parseLocalDate(value);
  if (!date) return value;
  return `${date.getDate()}/${date.getMonth() + 1}`;
}

function parseLocalDate(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || '');
  if (!match) return null;
  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
}

function toLocalDateString(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

