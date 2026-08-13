import { Component, computed, inject, signal } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Appointment } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatGradeLabel } from '../../grade.util';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

const HOUR_START = 8;
const HOUR_END = 22;
const PX_PER_HOUR = 110;
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
  selector: 'app-teacher-appointments',
  imports: [PageFeedbackComponent, TranslatePipe],
  templateUrl: './teacher-appointments.component.html',
  styleUrl: '../admin/admin-panel.css'
})
export class TeacherAppointmentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly appointments = signal<Appointment[]>([]);
  readonly error = signal('');
  readonly weekStart = signal(startOfWeek(new Date()));
  readonly selectedDayKey = signal(dayKey(new Date()));

  readonly hours = Array.from({ length: HOUR_END - HOUR_START }, (_, i) => HOUR_START + i);
  readonly pxPerHour = PX_PER_HOUR;
  readonly hourStart = HOUR_START;
  readonly calendarHeight = (HOUR_END - HOUR_START) * PX_PER_HOUR;

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

  readonly selectedDayBlocks = computed(() =>
    layoutDayBlocks(this.appointmentsByDay().get(this.selectedDayKey()) ?? [])
  );

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getMyAppointments().subscribe({
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
        this.error.set('');
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.appointments.loadFailed'))
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

  appointmentLabel(appointment: Appointment): string {
    this.locale.lang();
    const teacher = (appointment.teacherName || '').trim();
    const grade = formatGradeLabel((k, p) => this.locale.t(k, p), appointment.courseGrade);
    const courseName = (appointment.courseName || '').trim();
    return [teacher, grade, courseName].filter(Boolean).join('-');
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

function normalizeAppointments(items: Appointment[] | null | undefined): Appointment[] {
  if (!Array.isArray(items)) return [];
  return items.map((item) => {
    const raw = item as Appointment & Record<string, unknown>;
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
      startsAtUtc: String(raw.startsAtUtc ?? raw['StartsAtUtc'] ?? ''),
      endsAtUtc: String(raw.endsAtUtc ?? raw['EndsAtUtc'] ?? ''),
      notes: String(raw.notes ?? raw['Notes'] ?? ''),
      label: String(raw.label ?? raw['Label'] ?? '')
    };
  });
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
