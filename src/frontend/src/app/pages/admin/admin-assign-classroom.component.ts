import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ClassroomCourseAssignment, Course, ManagedUser } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import {
  courseMatchesClassroomGrade,
  formatGradeLabel,
  teacherCoversGrade
} from '../../grade.util';

type CourseTeacherRow = { courseId: string; teacherId: string };

@Component({
  selector: 'app-admin-assign-classroom',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, TranslatePipe],
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

  readonly selectedGrade = computed(() => {
    const room = this.selectedClassroom();
    return room ? readOptionalGrade(room.grade) : null;
  });

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

  /** Courses for the selected classroom grade only. Empty until a graded classroom is chosen. */
  readonly courseOptions = computed(() => {
    this.locale.lang();
    const grade = this.selectedGrade();
    if (grade == null) return [];

    return this.allCourses()
      .filter((c) => courseMatchesClassroomGrade(c.grade, grade))
      .slice()
      .sort((a, b) => {
        const ga = a.grade ?? 999;
        const gb = b.grade ?? 999;
        if (ga !== gb) return ga - gb;
        return a.title.localeCompare(b.title);
      });
  });

  readonly hasGradeFilter = computed(() => this.selectedGrade() != null);

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getUsers().subscribe((users) => {
      this.allTeachers.set(users.filter((u) => u.role === 'Teacher'));
    });

    let coursesReady = false;
    let classroomsReady = false;
    let loadedClassrooms: Classroom[] = [];

    const applySelection = () => {
      if (!coursesReady || !classroomsReady) return;
      const selectedId = this.assignClassroomId();
      if (selectedId) this.loadRowsFromClassroom(selectedId, loadedClassrooms);
    };

    this.api.getCourses().subscribe((courses) => {
      this.allCourses.set((courses ?? []).map(normalizeCourse));
      coursesReady = true;
      applySelection();
    });
    this.api.getClassrooms().subscribe((classrooms) => {
      loadedClassrooms = (classrooms ?? []).map(normalizeClassroom);
      this.classrooms.set(loadedClassrooms);
      classroomsReady = true;
      applySelection();
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
    if (this.selectedGrade() == null) {
      this.error.set(this.locale.t('admin.assign.gradeRequired'));
      return;
    }
    const rows = this.courseRows();
    if (rows.some((r) => (r.courseId && !r.teacherId) || (!r.courseId && r.teacherId))) {
      this.error.set(this.locale.t('admin.classrooms.courseTeacherRequired'));
      return;
    }
    const allowed = new Set(this.courseOptions().map((c) => c.id));
    if (rows.some((r) => r.courseId && !allowed.has(r.courseId))) {
      this.error.set(this.locale.t('admin.assign.courseGradeMismatch'));
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

    const grade = readOptionalGrade(room.grade);
    const allowedCourseIds = new Set(
      this.allCourses()
        .filter((c) => courseMatchesClassroomGrade(c.grade, grade))
        .map((c) => c.id)
    );

    const courses = room.courses ?? [];
    if (courses.length) {
      const rows = courses
        .map((c) => ({
          courseId: c.courseId || '',
          teacherId: c.teacherId || ''
        }))
        .filter((r) => !r.courseId || grade == null || allowedCourseIds.has(r.courseId));
      this.courseRows.set(rows.length ? rows : [{ courseId: '', teacherId: '' }]);
      return;
    }

    if (room.courseId && (grade == null || allowedCourseIds.has(room.courseId))) {
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

function readOptionalGrade(value: unknown): number | null {
  if (value == null || value === '') return null;
  const n = Number(value);
  return Number.isNaN(n) ? null : n;
}

function normalizeCourse(course: Course): Course {
  const raw = course as Course & Record<string, unknown>;
  return {
    ...course,
    id: String(raw.id ?? raw['Id'] ?? ''),
    title: String(raw.title ?? raw['Title'] ?? ''),
    grade: readOptionalGrade(raw.grade ?? raw['Grade'])
  };
}

function normalizeClassroom(room: Classroom): Classroom {
  const raw = room as Classroom & Record<string, unknown>;
  const courses = Array.isArray(raw.courses)
    ? raw.courses
    : Array.isArray(raw['Courses'])
      ? (raw['Courses'] as Classroom['courses'])
      : [];
  return {
    ...room,
    id: String(raw.id ?? raw['Id'] ?? ''),
    name: String(raw.name ?? raw['Name'] ?? ''),
    grade: readOptionalGrade(raw.grade ?? raw['Grade']),
    courses: (courses ?? []).map((c) => {
      const link = c as NonNullable<Classroom['courses']>[number] & Record<string, unknown>;
      return {
        ...c,
        courseId: String(link.courseId ?? link['CourseId'] ?? ''),
        courseTitle: String(link.courseTitle ?? link['CourseTitle'] ?? ''),
        courseGrade: readOptionalGrade(link.courseGrade ?? link['CourseGrade']),
        courseSchoolType: String(link.courseSchoolType ?? link['CourseSchoolType'] ?? 'All'),
        teacherId: String(link.teacherId ?? link['TeacherId'] ?? ''),
        teacherName: String(link.teacherName ?? link['TeacherName'] ?? '')
      };
    })
  };
}
