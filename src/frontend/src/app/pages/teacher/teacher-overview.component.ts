import { Component, computed, inject, signal } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ClassroomDiagnosis, TeacherDashboard } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatCourseLabel, formatGradeLabel } from '../../grade.util';

type GradeStudentGroup = {
  grade: number | null;
  gradeLabel: string;
  courseTitles: string[];
  students: { studentId: string; displayName: string }[];
};

@Component({
  selector: 'app-teacher-overview',
  imports: [TranslatePipe],
  templateUrl: './teacher-overview.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherOverviewComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly dashboard = signal<TeacherDashboard | null>(null);
  readonly classrooms = signal<Classroom[]>([]);
  readonly diagnosis = signal<ClassroomDiagnosis | null>(null);

  readonly studentsByGrade = computed<GradeStudentGroup[]>(() => {
    this.locale.lang();
    const teacherId = this.dashboard()?.teacherId;
    if (!teacherId) return [];

    const byGrade = new Map<
      string,
      { grade: number | null; courses: Set<string>; students: Map<string, string> }
    >();

    for (const room of this.classrooms()) {
      const myCourses = (room.courses ?? []).filter((c) => c.teacherId === teacherId);
      const courseEntries =
        myCourses.length > 0
          ? myCourses
          : room.courseId
            ? [
                {
                  courseTitle: room.courseTitle || '',
                  courseGrade: room.courseGrade ?? room.grade ?? null
                }
              ]
            : [];

      if (!courseEntries.length) continue;

      for (const course of courseEntries) {
        const grade = course.courseGrade ?? room.grade ?? null;
        const key = grade == null ? 'all' : String(grade);
        let group = byGrade.get(key);
        if (!group) {
          group = { grade, courses: new Set(), students: new Map() };
          byGrade.set(key, group);
        }
        if (course.courseTitle) group.courses.add(course.courseTitle);
        for (const student of room.students ?? []) {
          group.students.set(student.studentId, student.displayName);
        }
      }
    }

    return [...byGrade.values()]
      .map((group) => ({
        grade: group.grade,
        gradeLabel: formatGradeLabel((k, p) => this.locale.t(k, p), group.grade),
        courseTitles: [...group.courses].sort((a, b) => a.localeCompare(b)),
        students: [...group.students.entries()]
          .map(([studentId, displayName]) => ({ studentId, displayName }))
          .sort((a, b) => a.displayName.localeCompare(b.displayName))
      }))
      .sort((a, b) => {
        if (a.grade == null && b.grade == null) return 0;
        if (a.grade == null) return 1;
        if (b.grade == null) return -1;
        return a.grade - b.grade;
      });
  });

  constructor() {
    this.api.getTeacherDashboard().subscribe((dashboard) => this.dashboard.set(dashboard));
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (classrooms[0]) this.loadDiagnosis(classrooms[0].id);
    });
  }

  courseDisplay(room: Classroom): string {
    const list = room.courses ?? [];
    if (list.length) {
      return list
        .map((c) => {
          const label = formatCourseLabel((k, p) => this.locale.t(k, p), c.courseTitle, c.courseGrade);
          return `${label} (${c.teacherName})`;
        })
        .join(', ');
    }
    if (!room.courseTitle) return this.locale.t('common.noCourse');
    return formatCourseLabel((k, p) => this.locale.t(k, p), room.courseTitle, room.courseGrade);
  }

  loadDiagnosis(classroomId: string): void {
    this.api.getClassroomDiagnosis(classroomId).subscribe({
      next: (diagnosis) => this.diagnosis.set(diagnosis),
      error: () => this.diagnosis.set(null)
    });
  }
}
