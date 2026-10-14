/** Time zones are IANA ids (e.g. Africa/Cairo); the browser does all conversions. */

const FALLBACK_ZONES = [
  'Africa/Cairo',
  'Asia/Riyadh',
  'Asia/Dubai',
  'Asia/Kuwait',
  'Asia/Qatar',
  'Asia/Amman',
  'Europe/London',
  'Europe/Berlin',
  'America/New_York',
  'America/Los_Angeles',
  'UTC'
];

export function browserTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
  } catch {
    return 'UTC';
  }
}

export function timeZoneIds(): string[] {
  try {
    const supported = (Intl as unknown as { supportedValuesOf?: (key: string) => string[] }).supportedValuesOf?.('timeZone');
    if (supported?.length) return supported;
  } catch {
    // Older browsers: fall through to the short list.
  }
  return FALLBACK_ZONES;
}

/** Minutes the zone is ahead of UTC at the given instant (Cairo in winter: 120). 0 for an unknown zone. */
export function zoneOffsetMinutes(zone: string, at: Date = new Date()): number {
  try {
    const parts = new Intl.DateTimeFormat('en-US', {
      timeZone: zone,
      hourCycle: 'h23',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    }).formatToParts(at);
    const part = (type: string) => Number(parts.find((p) => p.type === type)?.value);
    const asUtc = Date.UTC(part('year'), part('month') - 1, part('day'), part('hour'), part('minute'), part('second'));
    return Math.round((asUtc - at.getTime()) / 60000);
  } catch {
    return 0;
  }
}

/** Minutes to add to a wall-clock time in `fromZone` to get the wall-clock time in `toZone`. */
export function zoneShiftMinutes(fromZone: string, toZone: string, at: Date = new Date()): number {
  return zoneOffsetMinutes(toZone, at) - zoneOffsetMinutes(fromZone, at);
}

export function formatUtcOffset(offsetMinutes: number): string {
  const sign = offsetMinutes < 0 ? '-' : '+';
  const abs = Math.abs(offsetMinutes);
  return `UTC${sign}${String(Math.floor(abs / 60)).padStart(2, '0')}:${String(abs % 60).padStart(2, '0')}`;
}

/** Options for a time zone picker, labelled with the current UTC offset. */
export function timeZoneOptions(): { value: string; label: string }[] {
  const now = new Date();
  return timeZoneIds().map((zone) => ({
    value: zone,
    label: `${zone.replace(/_/g, ' ')} (${formatUtcOffset(zoneOffsetMinutes(zone, now))})`
  }));
}
