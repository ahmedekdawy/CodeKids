import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Course, CourseTerm } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import {
  MultiSelectOption,
  SearchableMultiSelectComponent
} from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';
import { GRADE_CODES, formatGradeLabel } from '../../grade.util';

@Component({
  selector: 'app-admin-courses',
  imports: [FormsModule, IconActionButtonComponent, SearchableMultiSelectComponent, TranslatePipe],
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
  /** Empty string = all courses; `'all'` = grade-null courses; otherwise a grade number. */
  readonly filterGrade = signal<number | '' | 'all'>('');

  readonly terms: CourseTerm[] = ['FirstTerm', 'SecondTerm', 'FullYear'];
  readonly grades = GRADE_CODES;
  readonly gradeOptions = computed<MultiSelectOption[]>(() => {
    this.locale.lang();
    return this.grades.map((g) => ({
      value: g,
      label: formatGradeLabel((k, p) => this.locale.t(k, p), g)
    }));
  });

  courseTitle = '';
  courseTheme = 'Adventure';
  courseDescription = '';
  courseAgeMin: number | null = 8;
  courseAgeMax: number | null = 12;
  courseTerm: CourseTerm | '' = '';
  /** Empty = one course for all grades; otherwise one course per selected grade. */
  courseGrades: number[] = [];
  courseSort: number | null = 10;

  editTitle = '';
  editTheme = '';
  editDescription = '';
  editAgeMin: number | null = 8;
  editAgeMax: number | null = 12;
  editTerm: CourseTerm | string | '' = '';
  editGrade: number | null = null;
  editSort: number | null = 0;

  readonly filteredCourses = computed(() => {
    const filter = this.filterGrade();
    if (filter === '') return this.courses();
    if (filter === 'all') return this.courses().filter((c) => c.grade == null);
    return this.courses().filter((c) => c.grade === filter);
  });

  readonly sortedCourses = computed(() =>
    sortBy(this.filteredCourses(), this.sortKey(), this.sortDir())
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

  onFilterGradeChange(value: string): void {
    if (value === '' || value === 'all') {
      this.filterGrade.set(value);
      return;
    }
    this.filterGrade.set(Number(value));
  }

  clearGradeFilter(): void {
    this.filterGrade.set('');
  }

  termLabel(term: string | null | undefined): string {
    if (!term) return this.locale.t('common.allTerms');
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

  gradeLabel(grade: number | null | undefined): string {
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  createCourse(): void {
    this.clearStatus();
    this.api
      .createCourse({
        title: this.courseTitle,
        theme: this.courseTheme,
        description: this.courseDescription,
        ageMin: this.courseAgeMin ?? 8,
        ageMax: this.courseAgeMax ?? 12,
        term: this.courseTerm || null,
        grades: this.courseGrades,
        sortOrder: this.courseSort ?? 0
      })
      .subscribe({
        next: (created) => {
          this.message.set(
            created.length === 1
              ? this.locale.t('admin.courses.created')
              : this.locale.t('admin.courses.createdMany', { n: created.length })
          );
          this.courseTitle = '';
          this.courseDescription = '';
          this.courseTerm = '';
          this.courseGrades = [];
          this.courseAgeMin = 8;
          this.courseAgeMax = 12;
          this.courseSort = 10;
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.courses.createFailed'))
      });
  }

  startEdit(course: Course): void {
    this.editingId.set(course.id);
    this.editTitle = course.title;
    this.editTheme = course.theme;
    this.editDescription = course.description;
    this.editAgeMin = course.ageMin;
    this.editAgeMax = course.ageMax;
    this.editTerm = course.term || '';
    this.editGrade = course.grade ?? null;
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
        ageMin: this.editAgeMin ?? 8,
        ageMax: this.editAgeMax ?? 12,
        term: this.editTerm || null,
        grade: this.editGrade,
        sortOrder: this.editSort ?? 0
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.courses.updated'));
          this.editingId.set(null);
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.courses.updateFailed'))
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
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.courses.deleteFailed'))
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
