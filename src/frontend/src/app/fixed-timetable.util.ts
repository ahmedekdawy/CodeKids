/** Fixed weekly timetable session slots with 45-minute lessons and 5-minute breaks. */

export const SESSION_MINUTES = 45;
export const BREAK_MINUTES = 5;
export const PERIOD_STEP_MINUTES = SESSION_MINUTES + BREAK_MINUTES;
export const DEFAULT_SESSION_COUNT = 6;
export const MIN_SESSION_COUNT = 1;
export const MAX_SESSION_COUNT = 12;
export const DEFAULT_AM_START_MINUTES = 8 * 60;
export const MIN_AM_START_MINUTES = 6 * 60;
export const MAX_AM_START_MINUTES = 14 * 60;
export const DEFAULT_PM_START_MINUTES = 15 * 60;
export const MIN_PM_START_MINUTES = 12 * 60;
export const MAX_PM_START_MINUTES = 21 * 60;
export const PM_START_MINUTES = DEFAULT_PM_START_MINUTES;

export type TimetablePeriodFilter = 'am' | 'pm' | '';

export type FixedSessionSlot = {
  sessionNumber: number;
  period: 'am' | 'pm';
  key: string;
  startMinutes: number;
  endMinutes: number;
  timeLabel: string;
  headerLabel: string;
};

export function sessionNumbers(count: number): number[] {
  const n = normalizeSessionCount(count);
  return Array.from({ length: n }, (_, i) => i + 1);
}

export function normalizeSessionCount(value: unknown, fallback = DEFAULT_SESSION_COUNT): number {
  const n = Number(value);
  if (!Number.isInteger(n)) return fallback;
  return Math.min(MAX_SESSION_COUNT, Math.max(MIN_SESSION_COUNT, n));
}

/**
 * @param startMinutes when the period's first session starts; each period has its own configured start.
 * @param shiftMinutes added to the displayed times only, to show school times in the viewer's time zone.
 *   A slot that lands on another day gets a (+1) / (-1) marker.
 */
export function buildSessionSlots(
  period: 'am' | 'pm',
  count = DEFAULT_SESSION_COUNT,
  periodStartMinutes: number = period === 'am' ? DEFAULT_AM_START_MINUTES : DEFAULT_PM_START_MINUTES,
  shiftMinutes = 0
): FixedSessionSlot[] {
  const base =
    period === 'am'
      ? normalizeAmStartMinutes(periodStartMinutes)
      : normalizePmStartMinutes(periodStartMinutes);
  return sessionNumbers(count).map((sessionNumber) => {
    const startMinutes = base + (sessionNumber - 1) * PERIOD_STEP_MINUTES;
    const endMinutes = startMinutes + SESSION_MINUTES;
    const dayOffset = Math.floor((startMinutes + shiftMinutes) / MINUTES_PER_DAY);
    const dayMarker = dayOffset === 0 ? '' : ` (${dayOffset > 0 ? '+' : '-'}${Math.abs(dayOffset)})`;
    const timeLabel =
      `${formatClock(wrapDay(startMinutes + shiftMinutes))}-${formatClock(wrapDay(endMinutes + shiftMinutes))}${dayMarker}`;
    return {
      sessionNumber,
      period,
      key: `${period}-${sessionNumber}`,
      startMinutes,
      endMinutes,
      timeLabel,
      headerLabel: `${sessionNumber}\n${timeLabel}`
    };
  });
}

export function visibleSessionSlots(
  periodFilter: TimetablePeriodFilter,
  amCount = DEFAULT_SESSION_COUNT,
  pmCount = DEFAULT_SESSION_COUNT,
  pmStartMinutes = DEFAULT_PM_START_MINUTES,
  shiftMinutes = 0,
  amStartMinutes = DEFAULT_AM_START_MINUTES
): FixedSessionSlot[] {
  const am = () => buildSessionSlots('am', amCount, amStartMinutes, shiftMinutes);
  const pm = () => buildSessionSlots('pm', pmCount, pmStartMinutes, shiftMinutes);
  if (periodFilter === 'am') return am();
  if (periodFilter === 'pm') return pm();
  return [...am(), ...pm()];
}

const MINUTES_PER_DAY = 24 * 60;

function wrapDay(totalMinutes: number): number {
  return ((totalMinutes % MINUTES_PER_DAY) + MINUTES_PER_DAY) % MINUTES_PER_DAY;
}

export function normalizeAmStartMinutes(value: unknown, fallback = DEFAULT_AM_START_MINUTES): number {
  const n = Number(value);
  if (!Number.isInteger(n) || n <= 0) return fallback;
  return Math.min(MAX_AM_START_MINUTES, Math.max(MIN_AM_START_MINUTES, n));
}

export function normalizePmStartMinutes(value: unknown, fallback = DEFAULT_PM_START_MINUTES): number {
  const n = Number(value);
  if (!Number.isInteger(n) || n <= 0) return fallback;
  return Math.min(MAX_PM_START_MINUTES, Math.max(MIN_PM_START_MINUTES, n));
}

/** `HH:MM` for a time-of-day input; the value is only wrapped into the day, not clamped to a shift. */
export function minutesToTimeInput(totalMinutes: number): string {
  const normalized = wrapDay(Math.round(Number(totalMinutes) || 0));
  const hour = Math.floor(normalized / 60);
  const minute = normalized % 60;
  return `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`;
}

/** Parses an `HH:MM` input; the caller decides which shift it belongs to and normalizes it. */
export function timeInputToMinutes(value: string, fallback: number): number {
  const match = /^(\d{1,2}):(\d{2})$/.exec((value ?? '').trim());
  if (!match) return fallback;
  const hour = Number(match[1]);
  const minute = Number(match[2]);
  if (hour > 23 || minute > 59) return fallback;
  return hour * 60 + minute;
}

export function formatClock(totalMinutes: number): string {
  const hour24 = Math.floor(totalMinutes / 60) % 24;
  const minute = ((totalMinutes % 60) + 60) % 60;
  const hour12 = hour24 % 12 === 0 ? 12 : hour24 % 12;
  if (minute === 0) return String(hour12);
  return `${hour12}:${String(minute).padStart(2, '0')}`;
}

/** Sunday-first week to match existing admin timetable day rows. */
export const WEEKDAY_INDEXES = [0, 1, 2, 3, 4, 5, 6] as const;

export function arabicWeekdayName(dayOfWeek: number): string {
  // 4 Jan 2026 is a Sunday — use a fixed Sunday-based week for labels.
  const sunday = new Date(2026, 0, 4);
  sunday.setDate(sunday.getDate() + dayOfWeek);
  return sunday.toLocaleDateString('ar', { weekday: 'long' });
}
