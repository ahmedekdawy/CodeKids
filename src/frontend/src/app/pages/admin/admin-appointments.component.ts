import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Appointment, Course, ManagedUser } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatGradeLabel } from '../../grade.util';

/** Working hours on the appointment calendar. */
const HOUR_START = 8;
const HOUR_END = 22;
const PX_PER_HOUR = 110;
/** Default course session length. */
const SESSION_MINUTES = 45;
const MIN_BLOCK_HEIGHT = 72;

interface CalendarBlock {
  appointment: Appointment;
  top: number;
  height: number;
  column: number;
  columns: number;
}

@Component({
  selector: 'app-admin-appointments',
  imports: [FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './admin-appointments.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminAppointmentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly teachers = signal<ManagedUser[]>([]);
  readonly courses = signal<Course[]>([]);
  readonly appointments = signal<Appointment[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly editingId = signal<string | null>(null);
  readonly weekStart = signal(startOfWeek(new Date()));
  readonly selectedDayKey = signal(dayKey(new Date()));
  readonly sessionMinutes = SESSION_MINUTES;

  teacherId = '';
  courseId = '';
  startsAtLocal = '';
  endsAtLocal = '';
  notes = '';

  readonly hours = Array.from({ length: HOUR_END - HOUR_START }, (_, i) => HOUR_START + i);
  readonly pxPerHour = PX_PER_HOUR;
  readonly hourStart = HOUR_START;

  readonly weekDays = computed(() => {
    const start = this.weekStart();
    return Array.from({ length: 7 }, (_, i) => {
      const date = addDays(start, i);
      return {
        date,
        key: dayKey(date),
        label: formatDayLabel(date, this.locale.lang())
      };
    });
  });

  readonly weekLabel = computed(() => {
    const days = this.weekDays();
    const first = days[0]?.date;
    const last = days[6]?.date;
    if (!first || !last) return '';
    const opts: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' };
    return `${first.toLocaleDateString(undefined, opts)} – ${last.toLocaleDateString(undefined, {
      ...opts,
      year: 'numeric'
    })}`;
  });

  readonly calendarHeight = (HOUR_END - HOUR_START) * PX_PER_HOUR;

  readonly appointmentsByDay = computed(() => {
    const map = new Map<string, Appointment[]>();
    for (const appointment of this.appointments()) {
      const start = parseApiDate(appointment.startsAtUtc);
      if (!start) continue;
      const key = dayKey(start);
      const list = map.get(key) ?? [];
      list.push(appointment);
      map.set(key, list);
    }
    for (const list of map.values()) {
      list.sort((a, b) => String(a.startsAtUtc).localeCompare(String(b.startsAtUtc)));
    }
    return map;
  });

  readonly selectedDayAppointments = computed(
    () => this.appointmentsByDay().get(this.selectedDayKey()) ?? []
  );

  /** Same-time / overlapping sessions laid out in side-by-side columns. */
  readonly selectedDayBlocks = computed(() => layoutDayBlocks(this.selectedDayAppointments()));

  constructor() {
    const start = nextSessionStart(new Date());
    const end = addMinutes(start, SESSION_MINUTES);
    this.startsAtLocal = toLocalInputValue(start);
    this.endsAtLocal = toLocalInputValue(end);

    this.api.getUsers('Teacher').subscribe({
      next: (teachers) =>
        this.teachers.set(teachers.slice().sort((a, b) => a.displayName.localeCompare(b.displayName))),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.appointments.loadFailed'))
    });
    this.api.getCourses().subscribe({
      next: (courses) =>
        this.courses.set(courses.slice().sort((a, b) => a.title.localeCompare(b.title))),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.appointments.loadFailed'))
    });
    this.reload();
  }

  onStartChange(value: string): void {
    this.startsAtLocal = value;
    const start = new Date(value);
    if (Number.isNaN(start.getTime())) return;
    this.endsAtLocal = toLocalInputValue(addMinutes(start, SESSION_MINUTES));
  }

  reload(): void {
    this.api.getAppointments().subscribe({
      next: (items) => {
        const from = this.weekStart().getTime();
        const to = addDays(this.weekStart(), 7).getTime();
        const weekItems = normalizeAppointments(items).filter((item) => {
          const start = parseApiDate(item.startsAtUtc);
          const end = parseApiDate(item.endsAtUtc);
          if (!start || !end) return false;
          return end.getTime() >= from && start.getTime() < to;
        });
        this.appointments.set(weekItems);
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.appointments.loadFailed'))
    });
  }

  prevWeek(): void {
    this.weekStart.set(addDays(this.weekStart(), -7));
    this.selectedDayKey.set(dayKey(this.weekStart()));
    this.reload();
  }

  nextWeek(): void {
    this.weekStart.set(addDays(this.weekStart(), 7));
    this.selectedDayKey.set(dayKey(this.weekStart()));
    this.reload();
  }

  goThisWeek(): void {
    this.weekStart.set(startOfWeek(new Date()));
    this.selectedDayKey.set(dayKey(new Date()));
    this.reload();
  }

  selectDay(key: string): void {
    this.selectedDayKey.set(key);
  }

  dayCount(key: string): number {
    return this.appointmentsByDay().get(key)?.length ?? 0;
  }

  blockStyle(block: CalendarBlock): { top: string; height: string; left: string; width: string } {
    const gap = 0.35;
    const width = (100 - gap * (block.columns + 1)) / block.columns;
    const left = gap + block.column * (width + gap);
    return {
      top: `${block.top}px`,
      height: `${block.height}px`,
      left: `${left}%`,
      width: `${width}%`
    };
  }

  formatTimeRange(appointment: Appointment): string {
    const start = parseApiDate(appointment.startsAtUtc);
    const end = parseApiDate(appointment.endsAtUtc);
    if (!start || !end) return this.locale.t('common.emDash');
    const opts: Intl.DateTimeFormatOptions = { hour: '2-digit', minute: '2-digit' };
    return `${start.toLocaleTimeString(undefined, opts)} – ${end.toLocaleTimeString(undefined, opts)}`;
  }

  hourLabel(hour: number): string {
    return `${String(hour).padStart(2, '0')}:00`;
  }

  courseLabel(course: Course): string {
    return `${formatGradeLabel((k, p) => this.locale.t(k, p), course.grade)} - ${course.title}`;
  }

  appointmentLabel(appointment: Appointment): string {
    const course = this.courses().find((c) => c.id === appointment.courseId);
    const teacher = (appointment.teacherName || '').trim();
    const grade = formatGradeLabel((k, p) => this.locale.t(k, p), course?.grade);
    const courseName = (course?.title || appointment.courseName || '').trim();
    return [teacher, grade, courseName].filter(Boolean).join('-');
  }

  createOrSave(): void {
    this.clearStatus();
    if (!this.teacherId || !this.courseId || !this.startsAtLocal || !this.endsAtLocal) {
      this.error.set(this.locale.t('admin.appointments.requiredFields'));
      return;
    }

    const startLocal = new Date(this.startsAtLocal);
    let endLocal = new Date(this.endsAtLocal);
    if (Number.isNaN(startLocal.getTime()) || Number.isNaN(endLocal.getTime())) {
      this.error.set(this.locale.t('admin.appointments.requiredFields'));
      return;
    }
    if (endLocal <= startLocal) {
      endLocal = addMinutes(startLocal, SESSION_MINUTES);
      this.endsAtLocal = toLocalInputValue(endLocal);
    }

    const payload = {
      teacherId: this.teacherId,
      courseId: this.courseId,
      startsAtUtc: startLocal.toISOString(),
      endsAtUtc: endLocal.toISOString(),
      notes: this.notes.trim() || null
    };

    const editingId = this.editingId();
    const request$ = editingId
      ? this.api.updateAppointment(editingId, payload)
      : this.api.createAppointment(payload);

    request$.subscribe({
      next: (saved) => {
        const normalized = normalizeAppointment(saved);
        this.message.set(
          this.locale.t(editingId ? 'admin.appointments.updated' : 'admin.appointments.created')
        );
        this.editingId.set(null);
        this.notes = '';

        const start = parseApiDate(normalized.startsAtUtc) ?? startLocal;
        this.selectedDayKey.set(dayKey(start));
        this.weekStart.set(startOfWeek(start));

        this.appointments.update((list) => {
          const without = list.filter((a) => a.id !== normalized.id);
          return [...without, normalized];
        });
        this.reload();
      },
      error: (err) =>
        this.error.set(
          this.locale.fromApiError(
            err,
            editingId ? 'admin.appointments.updateFailed' : 'admin.appointments.createFailed'
          )
        )
    });
  }

  startEdit(appointment: Appointment): void {
    const start = parseApiDate(appointment.startsAtUtc);
    const end = parseApiDate(appointment.endsAtUtc);
    this.editingId.set(appointment.id);
    this.teacherId = appointment.teacherId;
    this.courseId = appointment.courseId;
    this.startsAtLocal = start ? toLocalInputValue(start) : '';
    this.endsAtLocal = end ? toLocalInputValue(end) : '';
    this.notes = appointment.notes || '';
    if (start) this.selectedDayKey.set(dayKey(start));
    this.clearStatus();
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.notes = '';
  }

  deleteAppointment(appointment: Appointment): void {
    if (!confirm(this.locale.t('admin.appointments.confirmDelete', { label: this.appointmentLabel(appointment) }))) {
      return;
    }
    this.clearStatus();
    this.api.deleteAppointment(appointment.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.appointments.deleted'));
        if (this.editingId() === appointment.id) this.cancelEdit();
        this.appointments.update((list) => list.filter((a) => a.id !== appointment.id));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.appointments.deleteFailed'))
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}

function layoutDayBlocks(appointments: Appointment[]): CalendarBlock[] {
  const timed = appointments
    .map((appointment) => {
      const start = parseApiDate(appointment.startsAtUtc);
      const end = parseApiDate(appointment.endsAtUtc);
      if (!start || !end) return null;
      const startMinutes = start.getHours() * 60 + start.getMinutes() + start.getSeconds() / 60;
      const endMinutes = Math.max(
        end.getHours() * 60 + end.getMinutes() + end.getSeconds() / 60,
        startMinutes + SESSION_MINUTES
      );
      return { appointment, startMinutes, endMinutes };
    })
    .filter((x): x is { appointment: Appointment; startMinutes: number; endMinutes: number } => !!x)
    .sort((a, b) => a.startMinutes - b.startMinutes || a.endMinutes - b.endMinutes);

  if (!timed.length) return [];

  const dayStart = HOUR_START * 60;
  const dayEnd = HOUR_END * 60;
  const blocks: CalendarBlock[] = [];

  let cluster: typeof timed = [];
  let clusterEnd = -1;

  const flushCluster = () => {
    if (!cluster.length) return;

    const columnEnds: number[] = [];
    const placed: { item: (typeof timed)[number]; column: number }[] = [];

    for (const item of cluster) {
      let column = columnEnds.findIndex((end) => end <= item.startMinutes);
      if (column < 0) {
        column = columnEnds.length;
        columnEnds.push(item.endMinutes);
      } else {
        columnEnds[column] = item.endMinutes;
      }
      placed.push({ item, column });
    }

    const columns = Math.max(1, columnEnds.length);
    for (const { item, column } of placed) {
      const clampedStart = Math.min(Math.max(item.startMinutes, dayStart), dayEnd - 15);
      const clampedEnd = Math.min(Math.max(item.endMinutes, clampedStart + 15), dayEnd);
      const top = ((clampedStart - dayStart) / 60) * PX_PER_HOUR;
      const height = Math.max(((clampedEnd - clampedStart) / 60) * PX_PER_HOUR, MIN_BLOCK_HEIGHT);
      blocks.push({
        appointment: item.appointment,
        top,
        height,
        column,
        columns
      });
    }

    cluster = [];
    clusterEnd = -1;
  };

  for (const item of timed) {
    if (!cluster.length || item.startMinutes < clusterEnd) {
      cluster.push(item);
      clusterEnd = Math.max(clusterEnd, item.endMinutes);
    } else {
      flushCluster();
      cluster.push(item);
      clusterEnd = item.endMinutes;
    }
  }
  flushCluster();

  return blocks;
}

function nextSessionStart(now: Date): Date {
  const start = new Date(now);
  start.setSeconds(0, 0);
  const minutes = start.getMinutes();
  if (minutes === 0 || minutes === 15 || minutes === 30 || minutes === 45) {
    // keep
  } else if (minutes < 15) start.setMinutes(15);
  else if (minutes < 30) start.setMinutes(30);
  else if (minutes < 45) start.setMinutes(45);
  else {
    start.setHours(start.getHours() + 1);
    start.setMinutes(0);
  }
  if (start.getTime() <= now.getTime()) {
    start.setMinutes(start.getMinutes() + 15);
  }
  if (start.getHours() < HOUR_START) {
    start.setHours(HOUR_START, 0, 0, 0);
  }
  if (start.getHours() >= HOUR_END) {
    start.setDate(start.getDate() + 1);
    start.setHours(HOUR_START, 0, 0, 0);
  }
  return start;
}

function addMinutes(date: Date, minutes: number): Date {
  return new Date(date.getTime() + minutes * 60_000);
}

function normalizeAppointments(items: Appointment[] | null | undefined): Appointment[] {
  if (!Array.isArray(items)) return [];
  return items.map(normalizeAppointment);
}

function normalizeAppointment(item: Appointment): Appointment {
  const raw = item as Appointment & Record<string, unknown>;
  return {
    id: String(raw.id ?? raw['Id'] ?? ''),
    teacherId: String(raw.teacherId ?? raw['TeacherId'] ?? ''),
    teacherName: String(raw.teacherName ?? raw['TeacherName'] ?? ''),
    courseId: String(raw.courseId ?? raw['CourseId'] ?? ''),
    courseName: String(raw.courseName ?? raw['CourseName'] ?? ''),
    startsAtUtc: String(raw.startsAtUtc ?? raw['StartsAtUtc'] ?? ''),
    endsAtUtc: String(raw.endsAtUtc ?? raw['EndsAtUtc'] ?? ''),
    notes: String(raw.notes ?? raw['Notes'] ?? ''),
    label: String(raw.label ?? raw['Label'] ?? '')
  };
}

function parseApiDate(value: string | Date | null | undefined): Date | null {
  if (!value) return null;
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

function startOfWeek(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  d.setDate(d.getDate() + diff);
  return d;
}

function addDays(date: Date, days: number): Date {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}

function dayKey(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function formatDayLabel(date: Date, lang: string): string {
  return date.toLocaleDateString(lang === 'ar' ? 'ar' : 'en', {
    weekday: 'short',
    month: 'short',
    day: 'numeric'
  });
}

function toLocalInputValue(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

