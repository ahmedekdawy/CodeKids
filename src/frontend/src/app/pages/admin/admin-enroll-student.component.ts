import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ManagedUser } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';
import { formatGradeLabel, matchesStudentSchoolType } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { SearchableMultiSelectComponent } from '../../shared/searchable-multi-select/searchable-multi-select.component';

interface EnrollmentRow {
  classroomId: string;
  classroomName: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  coursesLabel: string;
}

@Component({
  selector: 'app-admin-enroll-student',
  imports: [SearchableSelectComponent, SearchableMultiSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './admin-enroll-student.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminEnrollStudentComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly students = signal<ManagedUser[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('classroomName');
  readonly sortDir = signal<SortDir>('asc');
  readonly enrollStudentId = signal('');
  readonly enrollClassroomId = signal('');
  readonly enrollCourseIds = signal<string[]>([]);

  readonly selectedStudent = computed(() =>
    this.students().find((s) => s.id === this.enrollStudentId()) ?? null
  );

  /** Classrooms whose courses match the student grade (or all-grades / no courses). */
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
          (c.courseGrade == null || grade == null || c.courseGrade === grade) &&
          matchesStudentSchoolType(c.courseSchoolType, schoolType)
      );
    }
    if (room.courseId) {
      if (
        (room.courseGrade == null || grade == null || room.courseGrade === grade) &&
        matchesStudentSchoolType(room.courseSchoolType, schoolType)
      ) {
        return [
          {
            courseId: room.courseId,
            courseTitle: room.courseTitle ?? room.courseId,
            courseGrade: room.courseGrade ?? null,
            courseSchoolType: room.courseSchoolType ?? 'All',
            teacherId: '',
            teacherName: ''
          }
        ];
      }
    }
    return [];
  });

  readonly enrollmentRows = computed(() => {
    const rows: EnrollmentRow[] = [];
    for (const room of this.classrooms()) {
      for (const student of room.students) {
        rows.push({
          classroomId: room.id,
          classroomName: room.name,
          studentId: student.studentId,
          studentName: student.displayName,
          studentEmail: student.email,
          coursesLabel:
            student.enrolledCourseTitles && student.enrolledCourseTitles.length
              ? student.enrolledCourseTitles.join(', ')
              : this.locale.t('admin.enroll.allGradeCourses')
        });
      }
    }
    return sortBy(rows, this.sortKey(), this.sortDir());
  });

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getUsers().subscribe((users) => {
      this.students.set(users.filter((u) => u.role === 'Student'));
    });
    this.api.getClassrooms().subscribe((classrooms) => this.classrooms.set(classrooms));
  }

  setSort(key: string): void {
    this.sortDir.set(nextSort(this.sortKey(), key, this.sortDir()));
    this.sortKey.set(key);
  }

  sortMark(key: string): string {
    if (this.sortKey() !== key) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
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
          (room.courseGrade == null || studentGrade == null || room.courseGrade === studentGrade) &&
          matchesStudentSchoolType(room.courseSchoolType, studentSchoolType)
        );
      }
      return true;
    }
    return courses.some(
      (c) =>
        (c.courseGrade == null || studentGrade == null || c.courseGrade === studentGrade) &&
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

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
