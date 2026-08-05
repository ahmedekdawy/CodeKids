import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, Course, ManagedUser } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';

@Component({
  selector: 'app-admin-assign-classroom',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './admin-assign-classroom.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminAssignClassroomComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly teachers = signal<ManagedUser[]>([]);
  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('name');
  readonly sortDir = signal<SortDir>('asc');

  assignClassroomId = '';
  assignTeacherId = '';
  assignCourseId = '';

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

  assignClassroom(): void {
    this.message.set('');
    this.error.set('');
    if (!this.assignClassroomId) {
      this.error.set(this.locale.t('admin.assign.selectClassroom'));
      return;
    }
    this.api
      .assignClassroom(this.assignClassroomId, {
        teacherId: this.assignTeacherId || null,
        courseId: this.assignCourseId || null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.assign.updated'));
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err,'admin.assign.assignFailed'))
      });
  }
}
