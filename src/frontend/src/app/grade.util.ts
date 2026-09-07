/** Grade codes: KG1 = -1, KG2 = 0, then 1–12. null = all grades. */
export const GRADE_CODES: readonly number[] = [-1, 0, ...Array.from({ length: 12 }, (_, i) => i + 1)];

/** Stage bands: 0=KG, 1=grades 1–6, 2=7–9, 3=10–12. */
export const STAGE_CODES: readonly number[] = [0, 1, 2, 3];

export function gradeToStage(grade: number | null | undefined): number | null {
  if (grade == null) return null;
  if (grade === -1 || grade === 0) return 0;
  if (grade >= 1 && grade <= 6) return 1;
  if (grade >= 7 && grade <= 9) return 2;
  if (grade >= 10 && grade <= 12) return 3;
  return null;
}

export function gradesForStage(stage: number): number[] {
  switch (stage) {
    case 0:
      return [-1, 0];
    case 1:
      return [1, 2, 3, 4, 5, 6];
    case 2:
      return [7, 8, 9];
    case 3:
      return [10, 11, 12];
    default:
      return [];
  }
}

export function formatStageLabel(
  t: (key: string, params?: Record<string, string | number>) => string,
  stage: number
): string {
  switch (stage) {
    case 0:
      return t('common.stage0');
    case 1:
      return t('common.stage1');
    case 2:
      return t('common.stage2');
    case 3:
      return t('common.stage3');
    default:
      return t('common.stageN', { n: stage });
  }
}

export function formatGradeLabel(
  t: (key: string, params?: Record<string, string | number>) => string,
  grade: number | null | undefined,
  allKey = 'common.allGrades'
): string {
  if (grade == null) return t(allKey);
  if (grade === -1) return t('common.gradeKg1');
  if (grade === 0) return t('common.gradeKg2');
  return t('common.gradeN', { n: grade });
}

export type ClassroomGradeSource = {
  grade?: number | null;
  courseGrade?: number | null;
  courses?: { courseGrade?: number | null }[] | null;
};

export function classroomEffectiveGrade(room: ClassroomGradeSource): number | null {
  if (room.grade != null) return room.grade;
  if (room.courseGrade != null) return room.courseGrade;
  const fromCourse = room.courses?.find((course) => course.courseGrade != null)?.courseGrade;
  return fromCourse ?? null;
}

export type GradeClassroomGroup<T extends { name: string }> = {
  grade: number | null;
  gradeLabel: string;
  classrooms: T[];
};

export function groupClassroomsByGrade<T extends ClassroomGradeSource & { name: string }>(
  classrooms: T[],
  t: (key: string, params?: Record<string, string | number>) => string
): GradeClassroomGroup<T>[] {
  const byGrade = new Map<string, GradeClassroomGroup<T>>();

  for (const room of classrooms) {
    const grade = classroomEffectiveGrade(room);
    const key = grade == null ? 'all' : String(grade);
    let group = byGrade.get(key);
    if (!group) {
      group = {
        grade,
        gradeLabel: formatGradeLabel(t, grade),
        classrooms: []
      };
      byGrade.set(key, group);
    }
    group.classrooms.push(room);
  }

  return [...byGrade.values()]
    .map((group) => ({
      ...group,
      classrooms: [...group.classrooms].sort((a, b) => a.name.localeCompare(b.name))
    }))
    .sort((a, b) => {
      if (a.grade == null && b.grade == null) return 0;
      if (a.grade == null) return 1;
      if (b.grade == null) return -1;
      return a.grade - b.grade;
    });
}

/** Display label for a course: "Grade - Title". */
export function formatCourseLabel(
  t: (key: string, params?: Record<string, string | number>) => string,
  title: string | null | undefined,
  grade?: number | null,
  allKey = 'common.allGrades',
  stageId?: number | null
): string {
  const name = (title || '').trim();
  if (!name) return '';
  return `${formatCourseAudienceLabel(t, grade, stageId, allKey)} - ${name}`;
}

/** Timetable cell label; combined grades override the course audience. */
export function formatTimetableCourseLine(
  t: (key: string, params?: Record<string, string | number>) => string,
  title: string | null | undefined,
  grade?: number | null,
  stageId?: number | null,
  combinedGrades?: number[] | null
): string {
  const grades = [...(combinedGrades ?? [])]
    .map((g) => Number(g))
    .filter((g) => Number.isFinite(g))
    .sort((a, b) => a - b);
  if (grades.length > 0) {
    const name = (title || '').trim();
    const names = grades.map((g) => formatGradeLabel(t, g)).join('، ');
    return name ? `${names} - ${name}` : names;
  }
  return formatCourseLabel(t, title, grade, 'common.allGrades', stageId);
}

export function formatCourseAudienceLabel(
  t: (key: string, params?: Record<string, string | number>) => string,
  grade?: number | null,
  stageId?: number | null,
  allKey = 'common.allGrades'
): string {
  if (grade != null) return formatGradeLabel(t, grade, allKey);
  if (stageId != null) return formatStageLabel(t, stageId);
  return t(allKey);
}

export function teacherCoversGrade(
  stages: number[] | null | undefined,
  grade: number | null | undefined
): boolean {
  const stage = gradeToStage(grade);
  if (stage == null) return true;
  const list = stages?.length ? stages : STAGE_CODES;
  return list.includes(stage);
}

export function teacherCoversCourse(
  stages: number[] | null | undefined,
  grade?: number | null,
  stageId?: number | null
): boolean {
  if (grade != null) return teacherCoversGrade(stages, grade);
  if (stageId != null) {
    const list = stages?.length ? stages : STAGE_CODES;
    return list.includes(Number(stageId));
  }
  return true;
}

export function courseMatchesClassroomGrade(
  courseGrade: number | null | undefined,
  classroomGrade: number | null | undefined,
  courseStageId?: number | null
): boolean {
  if (classroomGrade == null) return true;
  if (courseGrade != null) return Number(courseGrade) === Number(classroomGrade);
  if (courseStageId == null) return true;
  return gradeToStage(classroomGrade) === Number(courseStageId);
}

export function matchesStudentSchoolType(
  courseSchoolType?: string | null,
  studentSchoolType?: string | null
): boolean {
  const course = (courseSchoolType || 'All').trim().toLowerCase();
  if (!course || course === 'all') return true;
  const student = (studentSchoolType || '').trim().toLowerCase();
  if (!student || student === 'all') return true;
  return course === student;
}

