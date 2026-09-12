/** Visual helpers for the playful school timetable board. */

const PASTEL_COUNT = 8;

const SUBJECT_RULES: Array<{ test: RegExp; icon: string; tone: number }> = [
  { test: /math|رياض|حساب|جبر|هندس/i, icon: '🧮', tone: 0 },
  { test: /science|علوم|فيز|كيم|أحياء|بيول/i, icon: '🧪', tone: 1 },
  { test: /english|إنجل|انجلي/i, icon: '📘', tone: 2 },
  { test: /arabic|عربي|لغة عربية/i, icon: '📖', tone: 3 },
  { test: /ict|computer|حاسب|حاسوب|كمبيوتر|تكنولوجيا/i, icon: '💻', tone: 4 },
  { test: /دين|تربي[ةه] إسلام|اسلام|إسلام|religion/i, icon: '🕌', tone: 5 },
  { test: /قرآن|quran|قران/i, icon: '📗', tone: 5 },
  { test: /دراسات|اجتماع|جغراف|تاريخ|social/i, icon: '📚', tone: 6 },
  { test: /رسم|فن|art|تربي[ةه] فني/i, icon: '🎨', tone: 7 },
  { test: /رياض[ةه]|بدني|pe\b|sport/i, icon: '⚽', tone: 0 },
  { test: /فرنس|french/i, icon: '🇫🇷', tone: 2 },
  { test: /ألمان|المان|german/i, icon: '🇩🇪', tone: 6 },
  { test: /ذهن|mental/i, icon: '🧠', tone: 1 }
];

const DAY_ICONS = ['☀️', '💖', '⭐', '💙', '💛', '🌸', '🌙'] as const;

export function timetableSubjectTone(courseName: string | null | undefined): number {
  const name = (courseName ?? '').trim();
  for (const rule of SUBJECT_RULES) {
    if (rule.test.test(name)) return rule.tone;
  }
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = (hash * 31 + name.charCodeAt(i)) >>> 0;
  }
  return name ? hash % PASTEL_COUNT : 0;
}

export function timetableSubjectIcon(courseName: string | null | undefined): string {
  const name = (courseName ?? '').trim();
  for (const rule of SUBJECT_RULES) {
    if (rule.test.test(name)) return rule.icon;
  }
  return '📒';
}

export function timetableDayIcon(dayOfWeek: number): string {
  const index = ((dayOfWeek % DAY_ICONS.length) + DAY_ICONS.length) % DAY_ICONS.length;
  return DAY_ICONS[index];
}

export function timetableSessionOrdinalKey(sessionNumber: number): string {
  return `admin.timetable.sessionOrdinal.${sessionNumber}`;
}
