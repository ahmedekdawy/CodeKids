import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { totalPages } from '../../list-query.util';
import { Classroom, ClassroomEnrollmentListItem, ManagedUser } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort } from '../../sort.util';
import { courseMatchesClassroomGrade, formatGradeLabel, matchesStudentSchoolType } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { SearchableMultiSelectComponent } from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

interface EnrollmentRow {
  classroomId: string;
  classroomName: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  coursesLabel: string;
  enrolledCourseIds: string[];
}

@Component({
  selector: 'app-admin-enroll-student',
  imports: [PageFeedbackComponent, SearchableSelectComponent, SearchableMultiSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './admin-enroll-student.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminEnrollStudentComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private studentSearchTimer: ReturnType<typeof setTimeout> | null = null;

  readonly students = signal<ManagedUser[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly enrollmentRows = signal<EnrollmentRow[]>([]);
  readonly totalCount = signal(0);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('classroomName');
  readonly sortDir = signal<SortDir>('asc');
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly pageSizeOptions = [10, 25, 50];
  readonly enrollStudentId = signal('');
  readonly enrollClassroomId = signal('');
  readonly enrollCourseIds = signal<string[]>([]);
  readonly filterClassroomId = signal('');
  readonly filterCourseId = signal('');
  readonly filterStudent = signal('');

  readonly totalPages = computed(() => totalPages(this.totalCount(), this.pageSize()));

  readonly selectedStudent = computed(() =>
    this.students().find((s) => s.id === this.enrollStudentId()) ?? null
  );

  readonly enrollableClassrooms = computed(() => {
    const student = this.selectedStudent();
    const rooms = this.classrooms();
    if (!student) return rooms;
    const grade = student.grade ?? null;
    const schoolType = student.schoolType ?? null;
    return rooms.filter((room) => this.classroomMatchesStudent(room, grade, schoolType));
  });

  readonly enrollableCourses = computed(() => {
    const student = this.selectedStudent();
    const classroomId = this.enrollClassroomId();
    const room = this.classrooms().find((c) => c.id === classroomId);
    if (!room) return [];
    const grade = student?.grade ?? null;
    const schoolType = student?.schoolType ?? null;
    const assigned = room.courses ?? [];
    if (assigned.length) {
      return assigned.filter(
        (c) =>
          courseMatchesClassroomGrade(c.courseGrade ?? null, grade, c.courseStageId ?? null) &&
          matchesStudentSchoolType(c.courseSchoolType, schoolType)
      );
    }
    if (room.courseId) {
      if (
        courseMatchesClassroomGrade(room.courseGrade ?? null, grade, room.courseStageId ?? null) &&
        matchesStudentSchoolType(room.courseSchoolType, schoolType)
      ) {
        return [
          {
            courseId: room.courseId,
            courseTitle: room.courseTitle ?? room.courseId,
            courseGrade: room.courseGrade ?? null,
            courseStageId: room.courseStageId ?? null,
            courseSchoolType: room.courseSchoolType ?? 'All',
            teacherId: '',
            teacherName: ''
          }
        ];
      }
    }
    return [];
  });

  readonly classroomFilterOptions = computed(() =>
    this.classrooms()
      .slice()
      .sort((a, b) => a.name.localeCompare(b.name))
      .map((room) => ({ value: room.id, label: room.name }))
  );

  readonly courseFilterOptions = computed(() => {
    const classroomId = this.filterClassroomId();
    const rooms = classroomId
      ? this.classrooms().filter((room) => room.id === classroomId)
      : this.classrooms();
    const seen = new Map<string, string>();
    for (const room of rooms) {
      for (const course of room.courses ?? []) {
        if (course.courseId && !seen.has(course.courseId)) {
          seen.set(course.courseId, course.courseTitle || course.courseId);
        }
      }
      if (room.courseId && !seen.has(room.courseId)) {
        seen.set(room.courseId, room.courseTitle || room.courseId);
      }
    }
    return [...seen.entries()]
      .map(([value, label]) => ({ value, label }))
      .sort((a, b) => a.label.localeCompare(b.label));
  });

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getUsers().subscribe((users) => {
      this.students.set(users.filter((u) => u.role === 'Student'));
    });
    this.api.getClassrooms().subscribe((classrooms) => this.classrooms.set(classrooms));
    this.loadEnrollments();
  }

  loadEnrollments(): void {
    this.api
      .getClassroomEnrollments({
        classroomId: this.filterClassroomId() || undefined,
        courseId: this.filterCourseId() || undefined,
        studentSearch: this.filterStudent() || undefined,
        sortKey: this.sortKey(),
        sortDir: this.sortDir(),
        page: this.page(),
        pageSize: this.pageSize()
      })
      .subscribe({
        next: (result) => {
          this.totalCount.set(result.totalCount);
          if (this.page() > totalPages(result.totalCount, this.pageSize())) {
            this.page.set(Math.max(1, totalPages(result.totalCount, this.pageSize())));
            if (this.page() !== result.page) {
              this.loadEnrollments();
              return;
            }
          }
          this.enrollmentRows.set(result.items.map((item) => this.toRow(item)));
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.enroll.listFailed'))
      });
  }

  setSort(key: string): void {
    this.sortDir.set(nextSort(this.sortKey(), key, this.sortDir()));
    this.sortKey.set(key);
    this.page.set(1);
    this.loadEnrollments();
  }

  sortMark(key: string): string {
    if (this.sortKey() !== key) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  setFilterClassroom(classroomId: string): void {
    this.filterClassroomId.set(classroomId);
    const courseId = this.filterCourseId();
    if (courseId && !this.courseFilterOptions().some((option) => option.value === courseId)) {
      this.filterCourseId.set('');
    }
    this.page.set(1);
    this.loadEnrollments();
  }

  setFilterCourse(courseId: string): void {
    this.filterCourseId.set(courseId);
    this.page.set(1);
    this.loadEnrollments();
  }

  setFilterStudent(value: string): void {
    this.filterStudent.set(value);
    if (this.studentSearchTimer) clearTimeout(this.studentSearchTimer);
    this.studentSearchTimer = setTimeout(() => {
      this.page.set(1);
      this.loadEnrollments();
    }, 300);
  }

  setPageSize(value: string | number): void {
    this.pageSize.set(Number(value) || 10);
    this.page.set(1);
    this.loadEnrollments();
  }

  goToPage(nextPage: number): void {
    this.page.set(Math.min(Math.max(1, nextPage), this.totalPages()));
    this.loadEnrollments();
  }

  hasActiveFilters(): boolean {
    return !!(this.filterClassroomId() || this.filterCourseId() || this.filterStudent().trim());
  }

  resetFilters(): void {
    this.filterClassroomId.set('');
    this.filterCourseId.set('');
    this.filterStudent.set('');
    this.page.set(1);
    this.loadEnrollments();
  }

  studentLabel(student: ManagedUser): string {
    const extras: string[] = [];
    if (student.grade != null) {
      extras.push(formatGradeLabel((k, p) => this.locale.t(k, p), student.grade));
    }
    if (student.schoolType === 'Arabic') extras.push(this.locale.t('common.schoolTypeArabic'));
    if (student.schoolType === 'Language') extras.push(this.locale.t('common.schoolTypeLanguage'));
    if (!extras.length) return student.displayName;
    return `${student.displayName} (${extras.join(' · ')})`;
  }

  classroomMatchesStudent(
    room: Classroom,
    studentGrade: number | null,
    studentSchoolType: string | null
  ): boolean {
    const courses = room.courses ?? [];
    if (!courses.length) {
      if (room.courseId) {
        return (
          courseMatchesClassroomGrade(room.courseGrade ?? null, studentGrade, room.courseStageId ?? null) &&
          matchesStudentSchoolType(room.courseSchoolType, studentSchoolType)
        );
      }
      return true;
    }
    return courses.some(
      (c) =>
        courseMatchesClassroomGrade(c.courseGrade ?? null, studentGrade, c.courseStageId ?? null) &&
        matchesStudentSchoolType(c.courseSchoolType, studentSchoolType)
    );
  }

  onStudentChange(studentId: string): void {
    this.enrollStudentId.set(studentId);
    this.enrollCourseIds.set([]);
    const allowed = this.enrollableClassrooms();
    if (this.enrollClassroomId() && !allowed.some((r) => r.id === this.enrollClassroomId())) {
      this.enrollClassroomId.set('');
    }
  }

  onClassroomChange(classroomId: string): void {
    this.enrollClassroomId.set(classroomId);
    this.enrollCourseIds.set([]);
  }

  onCoursesChange(ids: (string | number)[] | null): void {
    this.enrollCourseIds.set((ids ?? []).map(String));
  }

  enrollStudent(): void {
    this.clearStatus();
    const classroomId = this.enrollClassroomId();
    const studentId = this.enrollStudentId();
    if (!classroomId || !studentId) {
      this.error.set(this.locale.t('admin.enroll.selectBoth'));
      return;
    }
    const student = this.selectedStudent();
    const room = this.classrooms().find((c) => c.id === classroomId);
    if (student && room && !this.classroomMatchesStudent(room, student.grade ?? null, student.schoolType ?? null)) {
      this.error.set(this.locale.t('admin.enroll.gradeMismatch'));
      return;
    }
    this.api.addStudentToClassroom(classroomId, studentId, this.enrollCourseIds()).subscribe({
      next: (result) => {
        this.message.set(this.locale.t('admin.enroll.enrolled', { status: result.whatsAppStatus }));
        this.enrollCourseIds.set([]);
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.enroll.enrollFailed'))
    });
  }

  removeEnrollment(row: EnrollmentRow): void {
    if (!confirm(this.locale.t('admin.enroll.confirmRemove', { student: row.studentName, classroom: row.classroomName }))) return;
    this.clearStatus();
    this.api.removeStudentFromClassroom(row.classroomId, row.studentId).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.enroll.removed'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.enroll.removeFailed'))
    });
  }

  private toRow(item: ClassroomEnrollmentListItem): EnrollmentRow {
    return {
      classroomId: item.classroomId,
      classroomName: item.classroomName,
      studentId: item.studentId,
      studentName: item.studentName,
      studentEmail: item.studentEmail,
      coursesLabel:
        item.enrolledCourseTitles?.length
          ? item.enrolledCourseTitles.join(', ')
          : this.locale.t('admin.enroll.allGradeCourses'),
      enrolledCourseIds: item.enrolledCourseIds ?? []
    };
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
