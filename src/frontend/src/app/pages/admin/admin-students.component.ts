import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { ManagedUser } from '../../models';
import { SortDir, nextSort, sortBy } from '../../sort.util';

@Component({
  selector: 'app-admin-students',
  imports: [FormsModule],
  templateUrl: './admin-students.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminStudentsComponent {
  private readonly api = inject(LearningApiService);
  readonly students = signal<ManagedUser[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('displayName');
  readonly sortDir = signal<SortDir>('asc');
  readonly editingId = signal<string | null>(null);

  studentEmail = '';
  studentName = '';
  studentPassword = '';
  studentParentId = '';
  studentMobile = '';

  editEmail = '';
  editName = '';
  editParentId = '';
  editPassword = '';
  editMobile = '';

  readonly sortedStudents = computed(() =>
    sortBy(this.students(), this.sortKey(), this.sortDir())
  );

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getUsers('Student').subscribe((students) => this.students.set(students));
  }

  setSort(key: string): void {
    this.sortDir.set(nextSort(this.sortKey(), key, this.sortDir()));
    this.sortKey.set(key);
  }

  sortMark(key: string): string {
    if (this.sortKey() !== key) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  createStudent(): void {
    this.clearStatus();
    this.api
      .createUser({
        email: this.studentEmail,
        displayName: this.studentName,
        password: this.studentPassword,
        role: 'Student',
        parentId: this.studentParentId || null,
        mobilePhone: this.studentMobile || null
      })
      .subscribe({
        next: () => {
          this.message.set('Student created.');
          this.studentEmail = '';
          this.studentName = '';
          this.studentPassword = '';
          this.studentMobile = '';
          this.reload();
        },
        error: (err) => this.error.set(err?.error?.message || 'Could not create student.')
      });
  }

  startEdit(student: ManagedUser): void {
    this.editingId.set(student.id);
    this.editEmail = student.email;
    this.editName = student.displayName;
    this.editParentId = student.parentId || '';
    this.editMobile = student.mobilePhone || '';
    this.editPassword = '';
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(studentId: string): void {
    this.clearStatus();
    this.api
      .updateUser(studentId, {
        email: this.editEmail,
        displayName: this.editName,
        role: 'Student',
        parentId: this.editParentId || null,
        password: this.editPassword || null,
        mobilePhone: this.editMobile || null
      })
      .subscribe({
        next: () => {
          this.message.set('Student updated.');
          this.editingId.set(null);
          this.reload();
        },
        error: (err) => this.error.set(err?.error?.message || 'Could not update student.')
      });
  }

  deleteStudent(student: ManagedUser): void {
    if (!confirm(`Delete student ${student.displayName}?`)) return;
    this.clearStatus();
    this.api.deleteUser(student.id).subscribe({
      next: () => {
        this.message.set('Student deleted.');
        this.reload();
      },
      error: (err) => this.error.set(err?.error?.message || 'Could not delete student.')
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
