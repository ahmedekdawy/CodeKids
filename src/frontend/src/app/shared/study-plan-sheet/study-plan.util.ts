import { WeeklyStudyPlan } from '../../models';

export function normalizeStudyPlans(items: WeeklyStudyPlan[] | null | undefined): WeeklyStudyPlan[] {
  if (!Array.isArray(items)) return [];
  return items.map(normalizeStudyPlan);
}

export function normalizeStudyPlan(item: WeeklyStudyPlan): WeeklyStudyPlan {
  const raw = item as WeeklyStudyPlan & Record<string, unknown>;
  const weeks = Array.isArray(raw.weeks)
    ? raw.weeks
    : Array.isArray(raw['Weeks'])
      ? (raw['Weeks'] as WeeklyStudyPlan['weeks'])
      : [];
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
    courseTerm: String(raw.courseTerm ?? raw['CourseTerm'] ?? '') || null,
    fromDate: String(raw.fromDate ?? raw['FromDate'] ?? ''),
    toDate: String(raw.toDate ?? raw['ToDate'] ?? ''),
    notes: String(raw.notes ?? raw['Notes'] ?? ''),
    weeks: weeks.map((week, index) => {
      const weekRaw = week as WeeklyStudyPlan['weeks'][number] & Record<string, unknown>;
      const topics = Array.isArray(weekRaw.topics)
        ? weekRaw.topics
        : Array.isArray(weekRaw['Topics'])
          ? (weekRaw['Topics'] as WeeklyStudyPlan['weeks'][number]['topics'])
          : [];
      return {
        id: String(weekRaw.id ?? weekRaw['Id'] ?? ''),
        weekNumber: Number(weekRaw.weekNumber ?? weekRaw['WeekNumber'] ?? index + 1),
        fromDate: String(weekRaw.fromDate ?? weekRaw['FromDate'] ?? ''),
        toDate: String(weekRaw.toDate ?? weekRaw['ToDate'] ?? ''),
        sortOrder: Number(weekRaw.sortOrder ?? weekRaw['SortOrder'] ?? index),
        topics: topics.map((topic, topicIndex) => {
          const topicRaw = topic as WeeklyStudyPlan['weeks'][number]['topics'][number] & Record<string, unknown>;
          return {
            id: String(topicRaw.id ?? topicRaw['Id'] ?? ''),
            title: String(topicRaw.title ?? topicRaw['Title'] ?? ''),
            highlight: Boolean(topicRaw.highlight ?? topicRaw['Highlight']),
            sortOrder: Number(topicRaw.sortOrder ?? topicRaw['SortOrder'] ?? topicIndex)
          };
        })
      };
    })
  };
}

export function academicYearFromRange(from: string, to: string): string {
  const start = parseLocalDate(from);
  const end = parseLocalDate(to) ?? start;
  if (!start) return String(new Date().getFullYear());
  const y1 = start.getFullYear();
  const y2 = end?.getFullYear() ?? y1;
  return y1 === y2 ? String(y1) : `${y1} - ${y2}`;
}

export function formatStudyPlanShortDate(value: string): string {
  const date = parseLocalDate(value);
  if (!date) return value;
  return `${date.getDate()}/${date.getMonth() + 1}`;
}

export function formatStudyPlanRange(from: string, to: string): string {
  return `${formatStudyPlanShortDate(from)} – ${formatStudyPlanShortDate(to)}`;
}

function parseLocalDate(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || '');
  if (!match) return null;
  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
}
