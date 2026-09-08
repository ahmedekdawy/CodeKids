import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { totalPages } from '../../list-query.util';
import { Classroom, ManagedUser, TeacherAssessmentReportItem } from '../../models';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-admin-teacher-assessments',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, TranslatePipe],
  templateUrl: './admin-teacher-assessments.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminTeacherAssessmentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly teachers = signal<ManagedUser[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly rows = signal<TeacherAssessmentReportItem[]>([]);
  readonly totalCount = signal(0);
  readonly message = signal('');
  readonly error = signal('');
  readonly busyId = signal<string | null>(null);

  readonly filterTeacherId = signal('');
  readonly filterClassroomId = signal('');
  readonly filterKind = signal('');
  readonly filterStatus = signal('');
  readonly filterFromDate = signal(startOfMonthLocal());
  readonly filterToDate = signal(endOfMonthLocal());
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly pageSizeOptions = [10, 25, 50];

  readonly totalPages = computed(() => totalPages(this.totalCount(), this.pageSize()));

  readonly teacherOptions = computed(() =>
    this.teachers()
      .slice()
      .sort((a, b) => a.displayName.localeCompare(b.displayName))
      .map((teacher) => ({ value: teacher.id, label: teacher.displayName }))
  );

  readonly classroomOptions = computed(() =>
    this.classrooms()
      .slice()
      .sort((a, b) => a.name.localeCompare(b.name))
      .map((room) => ({ value: room.id, label: room.name }))
  );

  readonly kindOptions = computed(() => {
    this.locale.lang();
    return [
      { value: 'Assignment', label: this.locale.t('admin.teacherAssessments.kindAssignment') },
      { value: 'Quiz', label: this.locale.t('admin.teacherAssessments.kindQuiz') },
      { value: 'Exam', label: this.locale.t('admin.teacherAssessments.kindExam') }
    ];
  });

  readonly statusOptions = computed(() => {
    this.locale.lang();
    return [
      { value: 'published', label: this.locale.t('admin.teacherAssessments.statusPublished') },
      { value: 'draft', label: this.locale.t('admin.teacherAssessments.statusDraft') }
    ];
  });

  constructor() {
    this.api.getUsers().subscribe((users) => {
      this.teachers.set(users.filter((u) => u.role === 'Teacher'));
    });
    this.api.getClassrooms().subscribe((rooms) => this.classrooms.set(rooms));
    this.reload();
  }

  hasActiveFilters(): boolean {
    return !!(
      this.filterTeacherId() ||
      this.filterClassroomId() ||
      this.filterKind() ||
      this.filterStatus() ||
      this.filterFromDate() !== startOfMonthLocal() ||
      this.filterToDate() !== endOfMonthLocal()
    );
  }

  setFilterTeacher(teacherId: string): void {
    this.filterTeacherId.set(teacherId);
  }

  setFilterClassroom(classroomId: string): void {
    this.filterClassroomId.set(classroomId);
  }

  setFilterKind(kind: string): void {
    this.filterKind.set(kind);
  }

  setFilterStatus(status: string): void {
    this.filterStatus.set(status);
  }

  setFilterFromDate(value: string): void {
    this.filterFromDate.set(value);
  }

  setFilterToDate(value: string): void {
    this.filterToDate.set(value);
  }

  setPageSize(value: string | number): void {
    this.pageSize.set(Number(value) || 10);
    this.page.set(1);
    this.reload();
  }

  goToPage(nextPage: number): void {
    this.page.set(Math.min(Math.max(1, nextPage), this.totalPages()));
    this.reload();
  }

  search(): void {
    this.page.set(1);
    this.reload();
  }

  resetFilters(): void {
    this.filterTeacherId.set('');
    this.filterClassroomId.set('');
    this.filterKind.set('');
    this.filterStatus.set('');
    this.filterFromDate.set(startOfMonthLocal());
    this.filterToDate.set(endOfMonthLocal());
    this.page.set(1);
    this.reload();
  }

  reload(): void {
    this.error.set('');
    this.api
      .getTeacherAssessmentsReport({
        teacherId: this.filterTeacherId() || undefined,
        classroomId: this.filterClassroomId() || undefined,
        fromDate: this.filterFromDate() || undefined,
        toDate: this.filterToDate() || undefined,
        status: this.filterStatus() || undefined,
        kind: this.filterKind() || undefined,
        page: this.page(),
        pageSize: this.pageSize()
      })
      .subscribe({
        next: (result) => {
          this.totalCount.set(result.totalCount);
          if (this.page() > totalPages(result.totalCount, this.pageSize())) {
            this.page.set(Math.max(1, totalPages(result.totalCount, this.pageSize())));
            if (this.page() !== result.page) {
              this.reload();
              return;
            }
          }
          this.rows.set(result.items);
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.teacherAssessments.loadFailed'))
      });
  }

  kindLabel(kind: string): string {
    if (kind === 'Assignment') return this.locale.t('admin.teacherAssessments.kindAssignment');
    if (kind === 'Quiz') return this.locale.t('admin.teacherAssessments.kindQuiz');
    if (kind === 'Exam') return this.locale.t('admin.teacherAssessments.kindExam');
    return kind;
  }

  formatDate(value: string): string {
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return value;
    return d.toLocaleString();
  }

  publish(row: TeacherAssessmentReportItem): void {
    if (row.isPublished || this.busyId()) return;
    this.message.set('');
    this.error.set('');
    this.busyId.set(row.id);

    const onOk = () => {
      this.busyId.set(null);
      this.message.set(this.locale.t('admin.teacherAssessments.published', { title: row.title }));
      this.reload();
    };
    const onErr = (err: unknown) => {
      this.busyId.set(null);
      this.error.set(this.locale.fromApiError(err, 'admin.teacherAssessments.publishFailed'));
    };

    if (row.kind === 'Assignment') {
      this.api.publishAssignment(row.id).subscribe({ next: onOk, error: onErr });
      return;
    }
    if (row.kind === 'Quiz') {
      this.api.publishQuiz(row.id).subscribe({ next: onOk, error: onErr });
      return;
    }
    this.api.publishExam(row.id).subscribe({ next: onOk, error: onErr });
  }
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
