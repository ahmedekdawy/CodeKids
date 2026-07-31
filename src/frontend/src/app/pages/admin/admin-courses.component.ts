import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { Course } from '../../models';
import { SortDir, nextSort, sortBy } from '../../sort.util';

@Component({
  selector: 'app-admin-courses',
  imports: [FormsModule],
  templateUrl: './admin-courses.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminCoursesComponent {
  private readonly api = inject(LearningApiService);
  readonly courses = signal<Course[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('title');
  readonly sortDir = signal<SortDir>('asc');
  readonly editingId = signal<string | null>(null);

  courseTitle = '';
  courseTheme = 'Adventure';
  courseDescription = '';
  courseAgeMin = 8;
  courseAgeMax = 12;
  courseSort = 10;

  editTitle = '';
  editTheme = '';
  editDescription = '';
  editAgeMin = 8;
  editAgeMax = 12;
  editSort = 0;

  readonly sortedCourses = computed(() =>
    sortBy(this.courses(), this.sortKey(), this.sortDir())
  );

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getCourses().subscribe((courses) => this.courses.set(courses));
  }

  setSort(key: string): void {
    this.sortDir.set(nextSort(this.sortKey(), key, this.sortDir()));
    this.sortKey.set(key);
  }

  sortMark(key: string): string {
    if (this.sortKey() !== key) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  createCourse(): void {
    this.clearStatus();
    this.api
      .createCourse({
        title: this.courseTitle,
        theme: this.courseTheme,
        description: this.courseDescription,
        ageMin: this.courseAgeMin,
        ageMax: this.courseAgeMax,
        sortOrder: this.courseSort
      })
      .subscribe({
        next: () => {
          this.message.set('Course created.');
          this.courseTitle = '';
          this.courseDescription = '';
          this.reload();
        },
        error: (err) => this.error.set(err?.error?.message || 'Could not create course.')
      });
  }

  startEdit(course: Course): void {
    this.editingId.set(course.id);
    this.editTitle = course.title;
    this.editTheme = course.theme;
    this.editDescription = course.description;
    this.editAgeMin = course.ageMin;
    this.editAgeMax = course.ageMax;
    this.editSort = course.sortOrder;
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(courseId: string): void {
    this.clearStatus();
    this.api
      .updateCourse(courseId, {
        title: this.editTitle,
        theme: this.editTheme,
        description: this.editDescription,
        ageMin: this.editAgeMin,
        ageMax: this.editAgeMax,
        sortOrder: this.editSort
      })
      .subscribe({
        next: () => {
          this.message.set('Course updated.');
          this.editingId.set(null);
          this.reload();
        },
        error: (err) => this.error.set(err?.error?.message || 'Could not update course.')
      });
  }

  deleteCourse(course: Course): void {
    if (!confirm(`Delete course ${course.title}?`)) return;
    this.clearStatus();
    this.api.deleteCourse(course.id).subscribe({
      next: () => {
        this.message.set('Course deleted.');
        this.reload();
      },
      error: (err) => this.error.set(err?.error?.message || 'Could not delete course.')
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
