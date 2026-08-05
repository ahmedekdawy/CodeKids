import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ManagedUser } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';

interface EnrollmentRow {
  classroomId: string;
  classroomName: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
}

@Component({
  selector: 'app-admin-enroll-student',
  imports: [FormsModule, IconActionButtonComponent, TranslatePipe],
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

  enrollClassroomId = '';
  enrollStudentId = '';

  readonly enrollmentRows = computed(() => {
    const rows: EnrollmentRow[] = [];
    for (const room of this.classrooms()) {
      for (const student of room.students) {
        rows.push({
          classroomId: room.id,
          classroomName: room.name,
          studentId: student.studentId,
          studentName: student.displayName,
          studentEmail: student.email
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

  enrollStudent(): void {
    this.clearStatus();
    if (!this.enrollClassroomId || !this.enrollStudentId) {
      this.error.set(this.locale.t('admin.enroll.selectBoth'));
      return;
    }
    this.api.addStudentToClassroom(this.enrollClassroomId, this.enrollStudentId).subscribe({
      next: (result) => {
        this.message.set(this.locale.t('admin.enroll.enrolled', { status: result.whatsAppStatus }));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err,'admin.enroll.enrollFailed'))
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
      error: (err) => this.error.set(this.locale.fromApiError(err,'admin.enroll.removeFailed'))
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
