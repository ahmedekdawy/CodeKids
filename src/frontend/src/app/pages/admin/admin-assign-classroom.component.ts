import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ClassroomCourseAssignment, Course, ManagedUser } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import {
  courseMatchesClassroomGrade,
  formatGradeLabel,
  teacherCoversGrade
} from '../../grade.util';

type CourseTeacherRow = { courseId: string; teacherId: string };

@Component({
  selector: 'app-admin-assign-classroom',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './admin-assign-classroom.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminAssignClassroomComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly allTeachers = signal<ManagedUser[]>([]);
  readonly allCourses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly message = signal('');
  readonly error = signal('');

  readonly assignClassroomId = signal('');
  readonly courseRows = signal<CourseTeacherRow[]>([{ courseId: '', teacherId: '' }]);

  readonly selectedClassroom = computed(() =>
    this.classrooms().find((c) => c.id === this.assignClassroomId()) ?? null
  );

  readonly selectedGrade = computed(() => this.selectedClassroom()?.grade ?? null);

  readonly teachers = computed(() => {
    const grade = this.selectedGrade();
    const selectedIds = new Set(
      this.courseRows()
        .map((r) => r.teacherId)
        .filter(Boolean)
    );
    return this.allTeachers()
      .filter((t) => selectedIds.has(t.id) || teacherCoversGrade(t.stages, grade))
      .slice()
      .sort((a, b) => a.displayName.localeCompare(b.displayName));
  });

  readonly courseOptions = computed(() => {
    this.locale.lang();
    const grade = this.selectedGrade();
    const selectedIds = new Set(
      this.courseRows()
        .map((r) => r.courseId)
        .filter(Boolean)
    );
    return this.allCourses()
      .filter((c) => selectedIds.has(c.id) || courseMatchesClassroomGrade(c.grade, grade))
      .slice()
      .sort((a, b) => {
        const ga = a.grade ?? 999;
        const gb = b.grade ?? 999;
        if (ga !== gb) return ga - gb;
        return a.title.localeCompare(b.title);
      });
  });

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getUsers().subscribe((users) => {
      this.allTeachers.set(users.filter((u) => u.role === 'Teacher'));
    });
    this.api.getCourses().subscribe((courses) => this.allCourses.set(courses));
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      const selectedId = this.assignClassroomId();
      if (selectedId) this.loadRowsFromClassroom(selectedId, classrooms);
    });
  }

  courseDisplay(title?: string | null, grade?: number | null): string {
    if (!title) return '';
    return `${formatGradeLabel((k, p) => this.locale.t(k, p), grade)} - ${title}`;
  }

  gradeLabel(grade?: number | null): string {
    if (grade == null) return this.locale.t('common.none');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  courseOptionLabel(course: Course): string {
    return this.courseDisplay(course.title, course.grade);
  }

  addCourseRow(): void {
    this.courseRows.update((rows) => [...rows, { courseId: '', teacherId: '' }]);
  }

  removeCourseRow(index: number): void {
    this.courseRows.update((rows) => {
      const next = rows.filter((_, i) => i !== index);
      return next.length ? next : [{ courseId: '', teacherId: '' }];
    });
  }

  onClassroomChange(classroomId: string): void {
    this.assignClassroomId.set(classroomId);
    this.clearStatus();
    this.loadRowsFromClassroom(classroomId, this.classrooms());
  }

  onRowCourseChange(index: number, courseId: string): void {
    this.courseRows.update((rows) =>
      rows.map((row, i) => (i === index ? { ...row, courseId } : row))
    );
  }

  onRowTeacherChange(index: number, teacherId: string): void {
    this.courseRows.update((rows) =>
      rows.map((row, i) => (i === index ? { ...row, teacherId } : row))
    );
  }

  assignClassroom(): void {
    this.clearStatus();
    if (!this.assignClassroomId()) {
      this.error.set(this.locale.t('admin.assign.selectClassroom'));
      return;
    }
    const rows = this.courseRows();
    if (rows.some((r) => (r.courseId && !r.teacherId) || (!r.courseId && r.teacherId))) {
      this.error.set(this.locale.t('admin.classrooms.courseTeacherRequired'));
      return;
    }
    this.api
      .assignClassroom(this.assignClassroomId(), {
        courses: this.toAssignments(rows)
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.assign.updated'));
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.assign.assignFailed'))
      });
  }

  private loadRowsFromClassroom(classroomId: string, classrooms: Classroom[]): void {
    if (!classroomId) {
      this.courseRows.set([{ courseId: '', teacherId: '' }]);
      return;
    }

    const room = classrooms.find((c) => c.id === classroomId);
    if (!room) {
      this.courseRows.set([{ courseId: '', teacherId: '' }]);
      return;
    }

    const courses = room.courses ?? [];
    if (courses.length) {
      this.courseRows.set(
        courses.map((c) => ({
          courseId: c.courseId || '',
          teacherId: c.teacherId || ''
        }))
      );
      return;
    }

    if (room.courseId) {
      this.courseRows.set([
        {
          courseId: room.courseId,
          teacherId: room.teachers?.[0]?.teacherId || ''
        }
      ]);
      return;
    }

    this.courseRows.set([{ courseId: '', teacherId: '' }]);
  }

  private toAssignments(rows: CourseTeacherRow[]): ClassroomCourseAssignment[] {
    return rows
      .filter((r) => r.courseId && r.teacherId)
      .map((r) => ({ courseId: r.courseId, teacherId: r.teacherId }));
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
