/** Fixed weekly timetable session slots with 45-minute lessons and 5-minute breaks. */

export const SESSION_MINUTES = 45;
export const BREAK_MINUTES = 5;
export const PERIOD_STEP_MINUTES = SESSION_MINUTES + BREAK_MINUTES;
export const DEFAULT_SESSION_COUNT = 6;
export const MIN_SESSION_COUNT = 1;
export const MAX_SESSION_COUNT = 12;
export const AM_START_MINUTES = 8 * 60;
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

export function buildSessionSlots(
  period: 'am' | 'pm',
  count = DEFAULT_SESSION_COUNT,
  pmStartMinutes = DEFAULT_PM_START_MINUTES
): FixedSessionSlot[] {
  const base = period === 'am' ? AM_START_MINUTES : normalizePmStartMinutes(pmStartMinutes);
  return sessionNumbers(count).map((sessionNumber) => {
    const startMinutes = base + (sessionNumber - 1) * PERIOD_STEP_MINUTES;
    const endMinutes = startMinutes + SESSION_MINUTES;
    const timeLabel = `${formatClock(startMinutes)}-${formatClock(endMinutes)}`;
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
  pmStartMinutes = DEFAULT_PM_START_MINUTES
): FixedSessionSlot[] {
  if (periodFilter === 'am') return buildSessionSlots('am', amCount, pmStartMinutes);
  if (periodFilter === 'pm') return buildSessionSlots('pm', pmCount, pmStartMinutes);
  return [
    ...buildSessionSlots('am', amCount, pmStartMinutes),
    ...buildSessionSlots('pm', pmCount, pmStartMinutes)
  ];
}

export function normalizePmStartMinutes(value: unknown, fallback = DEFAULT_PM_START_MINUTES): number {
  const n = Number(value);
  if (!Number.isInteger(n) || n <= 0) return fallback;
  return Math.min(MAX_PM_START_MINUTES, Math.max(MIN_PM_START_MINUTES, n));
}

export function minutesToTimeInput(totalMinutes: number): string {
  const normalized = normalizePmStartMinutes(totalMinutes);
  const hour = Math.floor(normalized / 60);
  const minute = normalized % 60;
  return `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`;
}

export function timeInputToMinutes(value: string, fallback = DEFAULT_PM_START_MINUTES): number {
  const match = /^(\d{1,2}):(\d{2})$/.exec((value ?? '').trim());
  if (!match) return fallback;
  return normalizePmStartMinutes(Number(match[1]) * 60 + Number(match[2]), fallback);
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
