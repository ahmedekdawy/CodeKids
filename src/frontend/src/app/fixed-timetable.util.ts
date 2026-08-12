/** Fixed weekly timetable session slots (1–6) with 45-minute lessons and 5-minute breaks. */

export const SESSION_MINUTES = 45;
export const BREAK_MINUTES = 5;
export const PERIOD_STEP_MINUTES = SESSION_MINUTES + BREAK_MINUTES;
export const SESSION_NUMBERS = [1, 2, 3, 4, 5, 6] as const;
export const AM_START_MINUTES = 8 * 60;
export const PM_START_MINUTES = 14 * 60;

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

export function buildSessionSlots(period: 'am' | 'pm'): FixedSessionSlot[] {
  const base = period === 'am' ? AM_START_MINUTES : PM_START_MINUTES;
  return SESSION_NUMBERS.map((sessionNumber) => {
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

export function visibleSessionSlots(periodFilter: TimetablePeriodFilter): FixedSessionSlot[] {
  if (periodFilter === 'am') return buildSessionSlots('am');
  if (periodFilter === 'pm') return buildSessionSlots('pm');
  return [...buildSessionSlots('am'), ...buildSessionSlots('pm')];
}

export function formatClock(totalMinutes: number): string {
  const hour = Math.floor(totalMinutes / 60);
  const minute = totalMinutes % 60;
  if (minute === 0) return String(hour);
  return `${hour}:${String(minute).padStart(2, '0')}`;
}

/** Sunday-first week to match existing admin timetable day rows. */
export const WEEKDAY_INDEXES = [0, 1, 2, 3, 4, 5, 6] as const;

export function arabicWeekdayName(dayOfWeek: number): string {
  // 4 Jan 2026 is a Sunday — use a fixed Sunday-based week for labels.
  const sunday = new Date(2026, 0, 4);
  sunday.setDate(sunday.getDate() + dayOfWeek);
  return sunday.toLocaleDateString('ar', { weekday: 'long' });
}
