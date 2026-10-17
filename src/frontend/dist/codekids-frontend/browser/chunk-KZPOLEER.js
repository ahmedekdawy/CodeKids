import {
  __spreadProps,
  __spreadValues
} from "./chunk-EMYLRMBO.js";

// src/app/grade.util.ts
var GRADE_CODES = [-1, 0, ...Array.from({ length: 12 }, (_, i) => i + 1)];
var STAGE_CODES = [0, 1, 2, 3];
function gradeToStage(grade) {
  if (grade == null)
    return null;
  if (grade === -1 || grade === 0)
    return 0;
  if (grade >= 1 && grade <= 6)
    return 1;
  if (grade >= 7 && grade <= 9)
    return 2;
  if (grade >= 10 && grade <= 12)
    return 3;
  return null;
}
function gradesForStage(stage) {
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
function formatStageLabel(t, stage) {
  switch (stage) {
    case 0:
      return t("common.stage0");
    case 1:
      return t("common.stage1");
    case 2:
      return t("common.stage2");
    case 3:
      return t("common.stage3");
    default:
      return t("common.stageN", { n: stage });
  }
}
function formatGradeLabel(t, grade, allKey = "common.allGrades") {
  if (grade == null)
    return t(allKey);
  if (grade === -1)
    return t("common.gradeKg1");
  if (grade === 0)
    return t("common.gradeKg2");
  return t("common.gradeN", { n: grade });
}
function classroomEffectiveGrade(room) {
  if (room.grade != null)
    return room.grade;
  if (room.courseGrade != null)
    return room.courseGrade;
  const fromCourse = room.courses?.find((course) => course.courseGrade != null)?.courseGrade;
  return fromCourse ?? null;
}
function groupClassroomsByGrade(classrooms, t) {
  const byGrade = /* @__PURE__ */ new Map();
  for (const room of classrooms) {
    const grade = classroomEffectiveGrade(room);
    const key = grade == null ? "all" : String(grade);
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
  return [...byGrade.values()].map((group) => __spreadProps(__spreadValues({}, group), {
    classrooms: [...group.classrooms].sort((a, b) => a.name.localeCompare(b.name))
  })).sort((a, b) => {
    if (a.grade == null && b.grade == null)
      return 0;
    if (a.grade == null)
      return 1;
    if (b.grade == null)
      return -1;
    return a.grade - b.grade;
  });
}
function formatCourseLabel(t, title, grade, allKey = "common.allGrades", stageId) {
  const name = (title || "").trim();
  if (!name)
    return "";
  return `${formatCourseAudienceLabel(t, grade, stageId, allKey)} - ${name}`;
}
function formatTimetableCourseLine(t, title, grade, stageId, combinedGrades, omitGrade = false) {
  const name = (title || "").trim();
  if (omitGrade)
    return name;
  const grades = [...combinedGrades ?? []].map((g) => Number(g)).filter((g) => Number.isFinite(g)).sort((a, b) => a - b);
  if (grades.length > 0) {
    const names = grades.map((g) => formatGradeLabel(t, g)).join("\u060C ");
    return name ? `${names} - ${name}` : names;
  }
  return formatCourseLabel(t, title, grade, "common.allGrades", stageId);
}
function formatCourseAudienceLabel(t, grade, stageId, allKey = "common.allGrades") {
  if (grade != null)
    return formatGradeLabel(t, grade, allKey);
  if (stageId != null)
    return formatStageLabel(t, stageId);
  return t(allKey);
}
function teacherCoversGrade(stages, grade) {
  const stage = gradeToStage(grade);
  if (stage == null)
    return true;
  const list = stages?.length ? stages : STAGE_CODES;
  return list.includes(stage);
}
function teacherCoversCourse(stages, grade, stageId) {
  if (grade != null)
    return teacherCoversGrade(stages, grade);
  if (stageId != null) {
    const list = stages?.length ? stages : STAGE_CODES;
    return list.includes(Number(stageId));
  }
  return true;
}
function courseMatchesClassroomGrade(courseGrade, classroomGrade, courseStageId) {
  if (classroomGrade == null)
    return true;
  if (courseGrade != null)
    return Number(courseGrade) === Number(classroomGrade);
  if (courseStageId == null)
    return true;
  return gradeToStage(classroomGrade) === Number(courseStageId);
}
function matchesStudentSchoolType(courseSchoolType, studentSchoolType) {
  const course = (courseSchoolType || "All").trim().toLowerCase();
  if (!course || course === "all")
    return true;
  const student = (studentSchoolType || "").trim().toLowerCase();
  if (!student || student === "all")
    return true;
  return course === student;
}

export {
  GRADE_CODES,
  STAGE_CODES,
  gradeToStage,
  gradesForStage,
  formatStageLabel,
  formatGradeLabel,
  classroomEffectiveGrade,
  groupClassroomsByGrade,
  formatCourseLabel,
  formatTimetableCourseLine,
  formatCourseAudienceLabel,
  teacherCoversCourse,
  courseMatchesClassroomGrade,
  matchesStudentSchoolType
};
//# sourceMappingURL=chunk-KZPOLEER.js.map
