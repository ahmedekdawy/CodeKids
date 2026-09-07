import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { totalPages } from '../../list-query.util';
import { Classroom, StudentAttendanceStatus, StudentClassroomAttendance } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort } from '../../sort.util';
import { GRADE_CODES, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-teacher-student-attendance',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './teacher-student-attendance.component.html',
  styleUrl: '../admin/admin-panel.css'
})
export class TeacherStudentAttendanceComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private studentSearchTimer: ReturnType<typeof setTimeout> | null = null;

  readonly classrooms = signal<Classroom[]>([]);
  readonly formStudents = signal<{ value: string; label: string }[]>([]);
  readonly loadingFormStudents = signal(false);
  readonly rows = signal<StudentClassroomAttendance[]>([]);
  readonly totalCount = signal(0);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('attendanceDate');
  readonly sortDir = signal<SortDir>('desc');
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly pageSizeOptions = [10, 25, 50];
  readonly filterClassroomId = signal('');
  readonly filterGradeId = signal<number | ''>('');
  readonly filterStudent = signal('');
  readonly filterFromDate = signal(startOfMonthLocal());
  readonly filterToDate = signal(endOfMonthLocal());

  formClassroomId = '';
  formStudentId = '';
  formDate = todayLocal();
  formStatus: StudentAttendanceStatus = 'Present';

  readonly totalPages = computed(() => totalPages(this.totalCount(), this.pageSize()));

  readonly classroomOptions = computed(() =>
    this.classrooms()
      .slice()
      .sort((a, b) => a.name.localeCompare(b.name))
      .map((room) => ({ value: room.id, label: room.name }))
  );

  readonly studentOptions = computed(() => this.formStudents());

  readonly statusOptions = computed(() => {
    this.locale.lang();
    return [
      { value: 'Present', label: this.locale.t('studentAttendance.statusPresent') },
      { value: 'Absent', label: this.locale.t('studentAttendance.statusAbsent') }
    ];
  });

  readonly gradeOptions = computed(() => {
    this.locale.lang();
    return GRADE_CODES.map((g) => ({
      value: g,
      label: formatGradeLabel((k, p) => this.locale.t(k, p), g)
    }));
  });

  constructor() {
    this.api.getClassrooms().subscribe({
      next: (rooms) => this.classrooms.set(rooms ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'studentAttendance.loadFailed'))
    });
    this.loadRows();
  }

  loadRows(): void {
    const gradeId = this.filterGradeId();
    this.api
      .getStudentAttendance({
        classroomId: this.filterClassroomId() || undefined,
        gradeId: gradeId === '' ? undefined : gradeId,
        fromDate: this.filterFromDate() || undefined,
        toDate: this.filterToDate() || undefined,
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
              this.loadRows();
              return;
            }
          }
          this.rows.set(result.items);
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'studentAttendance.loadFailed'))
      });
  }

  setSort(key: string): void {
    this.sortDir.set(nextSort(this.sortKey(), key, this.sortDir()));
    this.sortKey.set(key);
    this.page.set(1);
    this.loadRows();
  }

  sortMark(key: string): string {
    if (this.sortKey() !== key) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  onFormClassroomChange(classroomId: string): void {
    this.formClassroomId = classroomId;
    this.formStudentId = '';
    this.formStudents.set([]);
    if (!classroomId) {
      this.loadingFormStudents.set(false);
      return;
    }
    this.loadingFormStudents.set(true);
    this.api
      .getClassroomEnrollments({
        classroomId,
        sortKey: 'studentName',
        sortDir: 'asc',
        page: 1,
        pageSize: 100
      })
      .subscribe({
        next: (result) => {
          this.formStudents.set(
            (result.items ?? [])
              .slice()
              .sort((a, b) => a.studentName.localeCompare(b.studentName))
              .map((student) => ({ value: student.studentId, label: student.studentName }))
          );
          this.loadingFormStudents.set(false);
        },
        error: (err) => {
          this.loadingFormStudents.set(false);
          this.error.set(this.locale.fromApiError(err, 'studentAttendance.loadStudentsFailed'));
        }
      });
  }

  setFilterClassroom(classroomId: string): void {
    this.filterClassroomId.set(classroomId);
    this.page.set(1);
    this.loadRows();
  }

  setFilterGrade(value: string): void {
    this.filterGradeId.set(value === '' ? '' : Number(value));
    this.page.set(1);
    this.loadRows();
  }

  setFilterStudent(value: string): void {
    this.filterStudent.set(value);
    if (this.studentSearchTimer) clearTimeout(this.studentSearchTimer);
    this.studentSearchTimer = setTimeout(() => {
      this.page.set(1);
      this.loadRows();
    }, 300);
  }

  setFilterFromDate(value: string): void {
    this.filterFromDate.set(value);
    this.page.set(1);
    this.loadRows();
  }

  setFilterToDate(value: string): void {
    this.filterToDate.set(value);
    this.page.set(1);
    this.loadRows();
  }

  setPageSize(value: string | number): void {
    this.pageSize.set(Number(value) || 10);
    this.page.set(1);
    this.loadRows();
  }

  goToPage(nextPage: number): void {
    this.page.set(Math.min(Math.max(1, nextPage), this.totalPages()));
    this.loadRows();
  }

  resetFilters(): void {
    this.filterClassroomId.set('');
    this.filterGradeId.set('');
    this.filterStudent.set('');
    this.filterFromDate.set(startOfMonthLocal());
    this.filterToDate.set(endOfMonthLocal());
    this.page.set(1);
    this.loadRows();
  }

  save(): void {
    this.clearStatus();
    if (!this.formClassroomId || !this.formStudentId || !this.formDate) {
      this.error.set(this.locale.t('studentAttendance.formRequired'));
      return;
    }
    this.api
      .createStudentAttendance({
        studentId: this.formStudentId,
        classroomId: this.formClassroomId,
        attendanceDate: this.formDate,
        status: this.formStatus
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('studentAttendance.saved'));
          this.loadRows();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'studentAttendance.saveFailed'))
      });
  }

  remove(row: StudentClassroomAttendance): void {
    if (!confirm(this.locale.t('studentAttendance.confirmDelete', { name: row.studentName, date: row.attendanceDate }))) {
      return;
    }
    this.clearStatus();
    this.api.deleteStudentAttendance(row.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('studentAttendance.deleted'));
        this.loadRows();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'studentAttendance.deleteFailed'))
    });
  }

  statusLabel(status: string): string {
    return status === 'Absent'
      ? this.locale.t('studentAttendance.statusAbsent')
      : this.locale.t('studentAttendance.statusPresent');
  }

  gradeLabel(gradeId?: number | null): string {
    if (gradeId == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), gradeId);
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}

function todayLocal(): string {
  return toLocalDateString(new Date());
}

function startOfMonthLocal(): string {
  const d = new Date();
  return toLocalDateString(new Date(d.getFullYear(), d.getMonth(), 1));
}

function endOfMonthLocal(): string {
  const d = new Date();
  return toLocalDateString(new Date(d.getFullYear(), d.getMonth() + 1, 0));
}

function toLocalDateString(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}
