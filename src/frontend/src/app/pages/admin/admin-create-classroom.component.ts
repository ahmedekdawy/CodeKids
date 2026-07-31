import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, Course, ManagedUser } from '../../models';
import { SortDir, nextSort, sortBy } from '../../sort.util';

@Component({
  selector: 'app-admin-create-classroom',
  imports: [FormsModule],
  templateUrl: './admin-create-classroom.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminCreateClassroomComponent {
  private readonly api = inject(LearningApiService);
  readonly teachers = signal<ManagedUser[]>([]);
  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('name');
  readonly sortDir = signal<SortDir>('asc');
  readonly editingId = signal<string | null>(null);

  classroomName = '';
  classroomDescription = '';
  classroomTeacherId = '';
  classroomCourseId = '';
  classroomWhatsAppInvite = '';
  classroomWhatsAppPhones = '';

  editName = '';
  editDescription = '';
  editTeacherId = '';
  editCourseId = '';
  editWhatsAppInvite = '';
  editWhatsAppPhones = '';

  readonly sortedClassrooms = computed(() =>
    sortBy(this.classrooms(), this.sortKey(), this.sortDir())
  );

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getUsers().subscribe((users) => {
      this.teachers.set(users.filter((u) => u.role === 'Teacher'));
    });
    this.api.getCourses().subscribe((courses) => this.courses.set(courses));
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

  createClassroom(): void {
    this.clearStatus();
    this.api
      .createClassroom({
        name: this.classroomName,
        description: this.classroomDescription,
        teacherId: this.classroomTeacherId || null,
        courseId: this.classroomCourseId || null,
        whatsAppGroupInviteUrl: this.classroomWhatsAppInvite,
        whatsAppNotifyPhones: this.classroomWhatsAppPhones
      })
      .subscribe({
        next: () => {
          this.message.set('Classroom created.');
          this.classroomName = '';
          this.classroomDescription = '';
          this.classroomTeacherId = '';
          this.classroomCourseId = '';
          this.classroomWhatsAppInvite = '';
          this.classroomWhatsAppPhones = '';
          this.reload();
        },
        error: (err) => this.error.set(err?.error?.message || 'Could not create classroom.')
      });
  }

  startEdit(room: Classroom): void {
    this.editingId.set(room.id);
    this.editName = room.name;
    this.editDescription = room.description;
    this.editTeacherId = room.teacherId || '';
    this.editCourseId = room.courseId || '';
    this.editWhatsAppInvite = room.whatsAppGroupInviteUrl || '';
    this.editWhatsAppPhones = room.whatsAppNotifyPhones || '';
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(classroomId: string): void {
    this.clearStatus();
    this.api
      .updateClassroom(classroomId, {
        name: this.editName,
        description: this.editDescription,
        teacherId: this.editTeacherId || null,
        courseId: this.editCourseId || null,
        whatsAppGroupInviteUrl: this.editWhatsAppInvite,
        whatsAppNotifyPhones: this.editWhatsAppPhones
      })
      .subscribe({
        next: () => {
          this.message.set('Classroom updated.');
          this.editingId.set(null);
          this.reload();
        },
        error: (err) => this.error.set(err?.error?.message || 'Could not update classroom.')
      });
  }

  deleteClassroom(room: Classroom): void {
    if (!confirm(`Delete classroom ${room.name}?`)) return;
    this.clearStatus();
    this.api.deleteClassroom(room.id).subscribe({
      next: () => {
        this.message.set('Classroom deleted.');
        this.reload();
      },
      error: (err) => this.error.set(err?.error?.message || 'Could not delete classroom.')
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
