import { Component, ElementRef, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, Course, FixedTimetableEntry, ManagedUser } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { GRADE_CODES, formatCourseLabel, formatGradeLabel } from '../../grade.util';
import { downloadElementAsPng } from '../../export-image.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import {
  SESSION_NUMBERS,
  TimetablePeriodFilter,
  WEEKDAY_INDEXES,
  arabicWeekdayName,
  visibleSessionSlots
} from '../../fixed-timetable.util';

type TimetableCell = {
  entry: FixedTimetableEntry;
  courseLine: string;
  teacherName: string;
};

type TimetableDayRow = {
  dayOfWeek: number;
  label: string;
  cells: Record<string, TimetableCell[]>;
};

type SlotTarget = {
  dayOfWeek: number;
  sessionNumber: number;
  period: 'am' | 'pm';
};

@Component({
  selector: 'app-admin-timetable',
  imports: [SearchableSelectComponent, FormsModule, TranslatePipe, IconActionButtonComponent],
  templateUrl: './admin-timetable.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminTimetableComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly teachers = signal<ManagedUser[]>([]);
  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly entries = signal<FixedTimetableEntry[]>([]);
  readonly filterGrade = signal<number | ''>('');
  readonly filterTeacherId = signal('');
  readonly filterPeriod = signal<TimetablePeriodFilter>('');
  readonly editingId = signal<string | null>(null);
  readonly dialogOpen = signal(false);
  readonly dragOverKey = signal<string | null>(null);
  readonly message = signal('');
  readonly error = signal('');
  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly moving = signal(false);

  readonly timetableWrap = viewChild<ElementRef<HTMLElement>>('timetableWrap');

  readonly grades = GRADE_CODES;
  readonly sessionNumbers = SESSION_NUMBERS;
  readonly weekDays = WEEKDAY_INDEXES;

  teacherId = '';
  courseId = '';
  dayOfWeek: number | '' = '';
  sessionNumber: number | '' = '';
  period: 'am' | 'pm' | '' = '';

  readonly selectedTeacherId = signal('');
  private dragEntryId: string | null = null;

  readonly availableCourses = computed(() => {
    const teacherId = this.selectedTeacherId();
    if (!teacherId) return [];
    const courseIds = new Set(
      this.classrooms()
        .flatMap((room) => room.courses ?? [])
        .filter((link) => link.teacherId === teacherId)
        .map((link) => link.courseId)
    );
    const editingId = this.editingId();
    if (editingId) {
      const current = this.entries().find((e) => e.id === editingId);
      if (current?.teacherId === teacherId && current.courseId) {
        courseIds.add(current.courseId);
      }
    }
    return this.courses()
      .filter((course) => courseIds.has(course.id))
      .slice()
      .sort((a, b) => {
        const ga = a.grade ?? 999;
        const gb = b.grade ?? 999;
        if (ga !== gb) return ga - gb;
        return a.title.localeCompare(b.title);
      });
  });

  readonly sessionSlots = computed(() => visibleSessionSlots(this.filterPeriod()));

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
          ),
          teacherName: entry.teacherName || ''
        });
      }

      return {
        dayOfWeek,
        label: arabicWeekdayName(dayOfWeek),
        cells
      };
    });
  });

  readonly dialogSlotLabel = computed(() => {
    this.locale.lang();
    if (this.dayOfWeek === '' || this.sessionNumber === '' || !this.period) return '';
    const day = arabicWeekdayName(Number(this.dayOfWeek));
    const periodLabel = this.locale.t(
      this.period === 'pm' ? 'admin.timetable.pm' : 'admin.timetable.am'
    );
    return `${day} · ${periodLabel} · ${this.locale.t('admin.timetable.session')} ${this.sessionNumber}`;
  });

  constructor() {
    this.api.getUsers('Teacher').subscribe({
      next: (teachers) =>
        this.teachers.set(teachers.slice().sort((a, b) => a.displayName.localeCompare(b.displayName))),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.timetable.loadFailed'))
    });
    this.api.getCourses().subscribe({
      next: (courses) => this.courses.set(courses),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.timetable.loadFailed'))
    });
    this.api.getClassrooms().subscribe({
      next: (classrooms) => this.classrooms.set(classrooms),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.timetable.loadFailed'))
    });
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set('');
    const grade = this.filterGrade();
    const teacherId = this.filterTeacherId();
    const period = this.filterPeriod();
    this.api
      .getTimetableEntries({
        teacherId: teacherId || undefined,
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
          this.error.set(this.locale.fromApiError(err, 'admin.timetable.loadFailed'));
        }
      });
  }

  onTeacherChange(teacherId: string): void {
    this.teacherId = teacherId;
    this.selectedTeacherId.set(teacherId);
    if (this.courseId && !this.availableCourses().some((c) => c.id === this.courseId)) {
      this.courseId = '';
    }
  }

  onFilterGradeChange(value: string): void {
    this.filterGrade.set(value === '' ? '' : Number(value));
    this.reload();
  }

  onFilterTeacherChange(value: string): void {
    this.filterTeacherId.set(value);
    this.reload();
  }

  onFilterPeriodChange(value: string): void {
    this.filterPeriod.set(value === 'am' || value === 'pm' ? value : '');
    this.reload();
  }

  gradeLabel(grade: number): string {
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade);
  }

  dayLabel(dayOfWeek: number): string {
    return arabicWeekdayName(dayOfWeek);
  }

  shiftLabel(shift: 'am' | 'pm'): string {
    return this.locale.t(shift === 'am' ? 'admin.timetable.am' : 'admin.timetable.pm');
  }

  cellKey(dayOfWeek: number, period: 'am' | 'pm', sessionNumber: number): string {
    return `${dayOfWeek}-${period}-${sessionNumber}`;
  }

  openCreateDialog(): void {
    this.editingId.set(null);
    this.resetFormFields();
    const filterPeriod = this.filterPeriod();
    if (filterPeriod === 'am' || filterPeriod === 'pm') {
      this.period = filterPeriod;
    }
    this.dialogOpen.set(true);
    this.clearStatus();
  }

  openCreateForSlot(dayOfWeek: number, period: 'am' | 'pm', sessionNumber: number): void {
    this.editingId.set(null);
    this.resetFormFields();
    this.dayOfWeek = dayOfWeek;
    this.sessionNumber = sessionNumber;
    this.period = period;
    this.dialogOpen.set(true);
    this.clearStatus();
  }

  onCellDoubleClick(dayOfWeek: number, period: 'am' | 'pm', sessionNumber: number): void {
    this.openCreateForSlot(dayOfWeek, period, sessionNumber);
  }

  startEdit(entry: FixedTimetableEntry): void {
    this.editingId.set(entry.id);
    this.teacherId = entry.teacherId;
    this.selectedTeacherId.set(entry.teacherId);
    this.courseId = entry.courseId;
    this.dayOfWeek = entry.dayOfWeek;
    this.sessionNumber = entry.sessionNumber;
    this.period = normalizePeriod(entry.period);
    this.dialogOpen.set(true);
    this.clearStatus();
  }

  closeDialog(): void {
    this.dialogOpen.set(false);
    this.editingId.set(null);
    this.resetFormFields();
  }

  onEntryDragStart(event: DragEvent, entry: FixedTimetableEntry): void {
    this.dragEntryId = entry.id;
    event.dataTransfer?.setData('text/plain', entry.id);
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
    }
  }

  onEntryDragEnd(): void {
    this.dragEntryId = null;
    this.dragOverKey.set(null);
  }

  onCellDragOver(event: DragEvent, dayOfWeek: number, period: 'am' | 'pm', sessionNumber: number): void {
    if (!this.dragEntryId) return;
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
    this.dragOverKey.set(this.cellKey(dayOfWeek, period, sessionNumber));
  }

  onCellDragLeave(dayOfWeek: number, period: 'am' | 'pm', sessionNumber: number): void {
    const key = this.cellKey(dayOfWeek, period, sessionNumber);
    if (this.dragOverKey() === key) this.dragOverKey.set(null);
  }

  onCellDrop(event: DragEvent, dayOfWeek: number, period: 'am' | 'pm', sessionNumber: number): void {
    event.preventDefault();
    this.dragOverKey.set(null);
    const entryId =
      this.dragEntryId || event.dataTransfer?.getData('text/plain') || null;
    this.dragEntryId = null;
    if (!entryId || this.moving()) return;

    const entry = this.entries().find((e) => e.id === entryId);
    if (!entry) return;

    if (
      entry.dayOfWeek === dayOfWeek &&
      entry.sessionNumber === sessionNumber &&
      normalizePeriod(entry.period) === period
    ) {
      return;
    }

    this.moveEntry(entry, { dayOfWeek, sessionNumber, period });
  }

  async exportAsImage(): Promise<void> {
    const wrap = this.timetableWrap()?.nativeElement;
    if (!wrap || this.exporting()) return;
    this.exporting.set(true);
    this.clearStatus();
    try {
      await downloadElementAsPng(wrap, `timetable-${stamp()}.png`);
      this.message.set(this.locale.t('admin.timetable.exported'));
    } catch {
      this.error.set(this.locale.t('admin.timetable.exportFailed'));
    } finally {
      this.exporting.set(false);
    }
  }

  createOrSave(): void {
    this.clearStatus();
    if (
      !this.teacherId ||
      !this.courseId ||
      this.dayOfWeek === '' ||
      this.sessionNumber === '' ||
      !this.period
    ) {
      this.error.set(this.locale.t('admin.timetable.requiredFields'));
      return;
    }

    const payload = {
      teacherId: this.teacherId,
      courseId: this.courseId,
      dayOfWeek: Number(this.dayOfWeek),
      sessionNumber: Number(this.sessionNumber),
      period: this.period
    };

    const editingId = this.editingId();
    if (editingId) {
      this.api.updateTimetableEntry(editingId, payload).subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.timetable.updated'));
          this.closeDialog();
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.timetable.saveFailed'))
      });
      return;
    }

    this.api.createTimetableEntry(payload).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.timetable.created'));
        // Keep teacher/course; keep slot so another session can be added quickly.
        this.editingId.set(null);
        this.dialogOpen.set(false);
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.timetable.saveFailed'))
    });
  }

  deleteEntry(entry: FixedTimetableEntry): void {
    if (!confirm(this.locale.t('admin.timetable.confirmDelete', { label: entry.label || entry.courseName }))) {
      return;
    }
    this.clearStatus();
    this.api.deleteTimetableEntry(entry.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.timetable.deleted'));
        if (this.editingId() === entry.id) this.closeDialog();
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.timetable.deleteFailed'))
    });
  }

  private moveEntry(entry: FixedTimetableEntry, target: SlotTarget): void {
    this.clearStatus();
    this.moving.set(true);
    this.api
      .updateTimetableEntry(entry.id, {
        teacherId: entry.teacherId,
        courseId: entry.courseId,
        dayOfWeek: target.dayOfWeek,
        sessionNumber: target.sessionNumber,
        period: target.period
      })
      .subscribe({
        next: () => {
          this.moving.set(false);
          this.message.set(this.locale.t('admin.timetable.moved'));
          this.reload();
        },
        error: (err) => {
          this.moving.set(false);
          this.error.set(this.locale.fromApiError(err, 'admin.timetable.moveFailed'));
        }
      });
  }

  private resetFormFields(): void {
    this.teacherId = '';
    this.selectedTeacherId.set('');
    this.courseId = '';
    this.dayOfWeek = '';
    this.sessionNumber = '';
    this.period = '';
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
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
