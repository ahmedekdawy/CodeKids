import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Course, CourseTerm } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';

@Component({
  selector: 'app-admin-courses',
  imports: [FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './admin-courses.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminCoursesComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly courses = signal<Course[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('title');
  readonly sortDir = signal<SortDir>('asc');
  readonly editingId = signal<string | null>(null);

  readonly terms: CourseTerm[] = ['FirstTerm', 'SecondTerm', 'FullYear'];
  readonly grades = Array.from({ length: 12 }, (_, i) => i + 1);

  courseTitle = '';
  courseTheme = 'Adventure';
  courseDescription = '';
  courseAgeMin = 8;
  courseAgeMax = 12;
  courseTerm: CourseTerm = 'FullYear';
  courseGrade = 1;
  courseSort = 10;

  editTitle = '';
  editTheme = '';
  editDescription = '';
  editAgeMin = 8;
  editAgeMax = 12;
  editTerm: CourseTerm | string = 'FullYear';
  editGrade = 1;
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

  termLabel(term: string): string {
    switch (term) {
      case 'FirstTerm':
        return this.locale.t('term.first');
      case 'SecondTerm':
        return this.locale.t('term.second');
      case 'FullYear':
        return this.locale.t('term.full');
      default:
        return term;
    }
  }

  gradeLabel(grade: number): string {
    return this.locale.t('common.gradeN', { n: grade });
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
        term: this.courseTerm,
        grade: this.courseGrade,
        sortOrder: this.courseSort
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.courses.created'));
          this.courseTitle = '';
          this.courseDescription = '';
          this.courseTerm = 'FullYear';
          this.courseGrade = 1;
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err,'admin.courses.createFailed'))
      });
  }

  startEdit(course: Course): void {
    this.editingId.set(course.id);
    this.editTitle = course.title;
    this.editTheme = course.theme;
    this.editDescription = course.description;
    this.editAgeMin = course.ageMin;
    this.editAgeMax = course.ageMax;
    this.editTerm = course.term || 'FullYear';
    this.editGrade = course.grade || 1;
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
        term: this.editTerm,
        grade: this.editGrade,
        sortOrder: this.editSort
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.courses.updated'));
          this.editingId.set(null);
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err,'admin.courses.updateFailed'))
      });
  }

  deleteCourse(course: Course): void {
    if (!confirm(this.locale.t('admin.courses.confirmDelete', { title: course.title }))) return;
    this.clearStatus();
    this.api.deleteCourse(course.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.courses.deleted'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err,'admin.courses.deleteFailed'))
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
