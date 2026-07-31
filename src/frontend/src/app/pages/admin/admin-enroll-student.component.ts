import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ManagedUser } from '../../models';
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
  imports: [FormsModule],
  templateUrl: './admin-enroll-student.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminEnrollStudentComponent {
  private readonly api = inject(LearningApiService);
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
      this.error.set('Select classroom and student.');
      return;
    }
    this.api.addStudentToClassroom(this.enrollClassroomId, this.enrollStudentId).subscribe({
      next: () => {
        this.message.set('Student enrolled.');
        this.reload();
      },
      error: (err) => this.error.set(err?.error?.message || 'Could not enroll student.')
    });
  }

  removeEnrollment(row: EnrollmentRow): void {
    if (!confirm(`Remove ${row.studentName} from ${row.classroomName}?`)) return;
    this.clearStatus();
    this.api.removeStudentFromClassroom(row.classroomId, row.studentId).subscribe({
      next: () => {
        this.message.set('Student removed from classroom.');
        this.reload();
      },
      error: (err) => this.error.set(err?.error?.message || 'Could not remove student.')
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
