import { Component, ElementRef, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { FixedTimetableEntry } from '../../models';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { GRADE_CODES, formatCourseLabel, formatGradeLabel } from '../../grade.util';
import { downloadElementAsPng } from '../../export-image.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import {
  TimetablePeriodFilter,
  WEEKDAY_INDEXES,
  arabicWeekdayName,
  visibleSessionSlots
} from '../../fixed-timetable.util';

type TimetableCell = {
  entry: FixedTimetableEntry;
  courseLine: string;
};

type TimetableDayRow = {
  dayOfWeek: number;
  label: string;
  cells: Record<string, TimetableCell[]>;
};

@Component({
  selector: 'app-teacher-timetable',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, TranslatePipe],
  templateUrl: './teacher-timetable.component.html',
  styleUrl: '../admin/admin-panel.css'
})
export class TeacherTimetableComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly brand = inject(SiteBrandService);

  readonly entries = signal<FixedTimetableEntry[]>([]);
  readonly filterGrade = signal<number | ''>('');
  readonly filterPeriod = signal<TimetablePeriodFilter>('');
  readonly error = signal('');
  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly message = signal('');

  readonly timetableWrap = viewChild<ElementRef<HTMLElement>>('timetableWrap');

  readonly grades = GRADE_CODES;
  readonly weekDays = WEEKDAY_INDEXES;

  readonly sessionSlots = computed(() =>
    visibleSessionSlots(this.filterPeriod(), this.brand.amSessionCount(), this.brand.pmSessionCount())
  );
  readonly amSlots = computed(() => this.sessionSlots().filter((s) => s.period === 'am'));
  readonly pmSlots = computed(() => this.sessionSlots().filter((s) => s.period === 'pm'));

  readonly timetableDays = computed<TimetableDayRow[]>(() => {
    this.locale.lang();
    const slots = this.sessionSlots();
    return this.weekDays.map((dayOfWeek) => {
      const cells: Record<string, TimetableCell[]> = {};
      for (const slot of slots) {
        cells[slot.key] = [];
      }

      for (const entry of this.entries()) {
        if (entry.dayOfWeek !== dayOfWeek) continue;
        const period = normalizePeriod(entry.period);
        const key = `${period}-${entry.sessionNumber}`;
        if (!cells[key]) continue;
        cells[key].push({
          entry,
          courseLine: formatCourseLabel(
            (k, p) => this.locale.t(k, p),
            entry.courseName,
            entry.courseGrade
          )
        });
      }

      return {
        dayOfWeek,
        label: arabicWeekdayName(dayOfWeek),
        cells
      };
    });
  });

  constructor() {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set('');
    const grade = this.filterGrade();
    const period = this.filterPeriod();
    this.api
      .getMyTimetableEntries({
        grade: grade === '' ? undefined : grade,
        period: period || undefined
      })
      .subscribe({
        next: (items) => {
          this.entries.set(normalizeEntries(items));
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.timetable.loadFailed'));
        }
      });
  }

  onFilterGradeChange(value: string): void {
    this.filterGrade.set(value === '' ? '' : Number(value));
    this.reload();
  }

  onFilterPeriodChange(value: string): void {
    this.filterPeriod.set(value === 'am' || value === 'pm' ? value : '');
    this.reload();
  }

  gradeLabel(grade: number): string {
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  shiftLabel(shift: 'am' | 'pm'): string {
    return this.locale.t(shift === 'am' ? 'admin.timetable.am' : 'admin.timetable.pm');
  }

  async exportAsImage(): Promise<void> {
    const wrap = this.timetableWrap()?.nativeElement;
    if (!wrap || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await downloadElementAsPng(wrap, `my-timetable-${stamp()}.png`);
      this.message.set(this.locale.t('admin.timetable.exported'));
    } catch {
      this.error.set(this.locale.t('admin.timetable.exportFailed'));
    } finally {
      this.exporting.set(false);
    }
  }
}

function normalizePeriod(value: string | null | undefined): 'am' | 'pm' {
  return String(value || '').toLowerCase() === 'pm' ? 'pm' : 'am';
}

function stamp(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}-${pad(d.getHours())}${pad(d.getMinutes())}`;
}

function normalizeEntries(items: FixedTimetableEntry[] | null | undefined): FixedTimetableEntry[] {
  if (!Array.isArray(items)) return [];
  return items.map((item) => {
    const raw = item as FixedTimetableEntry & Record<string, unknown>;
    return {
      id: String(raw.id ?? raw['Id'] ?? ''),
      teacherId: String(raw.teacherId ?? raw['TeacherId'] ?? ''),
      teacherName: String(raw.teacherName ?? raw['TeacherName'] ?? ''),
      courseId: String(raw.courseId ?? raw['CourseId'] ?? ''),
      courseName: String(raw.courseName ?? raw['CourseName'] ?? ''),
      courseGrade:
        raw.courseGrade == null && raw['CourseGrade'] == null
          ? null
          : Number(raw.courseGrade ?? raw['CourseGrade']),
      dayOfWeek: Number(raw.dayOfWeek ?? raw['DayOfWeek'] ?? 0),
      sessionNumber: Number(raw.sessionNumber ?? raw['SessionNumber'] ?? 0),
      period: normalizePeriod(String(raw.period ?? raw['Period'] ?? 'am')),
      label: String(raw.label ?? raw['Label'] ?? '')
    };
  });
}
