import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Course, CourseSchoolType, CourseTerm, Grade, Stage } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import {
  MultiSelectOption,
  SearchableMultiSelectComponent
} from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';
import { GRADE_CODES, formatCourseAudienceLabel, formatGradeLabel } from '../../grade.util';
import { includesIgnoreCase } from '../../list-query.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-admin-courses',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, IconActionButtonComponent, SearchableMultiSelectComponent, TranslatePipe],
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
  readonly filterName = signal('');
  /** Empty string = all stages. */
  readonly filterStage = signal<number | ''>('');
  /** Empty string = all grades. */
  readonly filterGrade = signal<number | ''>('');
  readonly stages = signal<Stage[]>([]);
  readonly catalogGrades = signal<Grade[]>([]);

  readonly terms: CourseTerm[] = ['FirstTerm', 'SecondTerm', 'FullYear'];
  readonly grades = GRADE_CODES;
  gradeOptions(): MultiSelectOption[] {
    this.locale.lang();
    const stageId = this.courseStageId;
    const catalog = this.catalogGrades().filter((g) => stageId === '' || g.stageId === stageId);
    if (!catalog.length) {
      return this.gradesForStage(stageId).map((g) => ({
        value: g,
        label: formatGradeLabel((k, p) => this.locale.t(k, p), g)
      }));
    }
    return catalog.map((g) => ({
      value: g.id,
      label: this.locale.lang() === 'ar' ? g.name : g.nameEn
    }));
  }

  editGradeOptions(): { value: number; label: string }[] {
    this.locale.lang();
    const stageId = this.editStageId;
    const catalog = this.catalogGrades().filter((g) => stageId === '' || g.stageId === Number(stageId));
    if (!catalog.length) {
      return this.gradesForStage(stageId === '' ? '' : Number(stageId)).map((g) => ({
        value: g,
        label: formatGradeLabel((k, p) => this.locale.t(k, p), g)
      }));
    }
    return catalog.map((g) => ({
      value: g.id,
      label: this.locale.lang() === 'ar' ? g.name : g.nameEn
    }));
  }

  courseTitle = '';
  courseTheme = 'Adventure';
  courseDescription = '';
  courseAgeMin: number | null = 8;
  courseAgeMax: number | null = 12;
  courseTerm: CourseTerm | '' = '';
  /** Empty = all grades / all grades in the selected stage. */
  courseGrades: number[] = [];
  courseStageId: number | '' = '';
  courseSchoolType: CourseSchoolType = 'All';
  courseSort: number | null = 10;

  editTitle = '';
  editTheme = '';
  editDescription = '';
  editAgeMin: number | null = 8;
  editAgeMax: number | null = 12;
  editTerm: CourseTerm | string | '' = '';
  editGrade: number | null = null;
  editStageId: number | '' = '';
  editSchoolType: CourseSchoolType = 'All';
  editSort: number | null = 0;

  readonly filterGradeOptions = computed(() => {
    this.locale.lang();
    const stageId = this.filterStage();
    const catalog = this.catalogGrades().filter((g) => stageId === '' || g.stageId === stageId);
    if (!catalog.length) {
      return this.gradesForStage(stageId).map((g) => ({
        value: g,
        label: formatGradeLabel((k, p) => this.locale.t(k, p), g)
      }));
    }
    return catalog.map((g) => ({
      value: g.id,
      label: this.locale.lang() === 'ar' ? g.name : g.nameEn
    }));
  });

  readonly filteredCourses = computed(() => {
    const name = this.filterName();
    const stage = this.filterStage();
    const grade = this.filterGrade();
    return this.courses().filter((course) => {
      if (!includesIgnoreCase(course.title, name)) return false;
      if (stage !== '' && !this.courseMatchesStage(course, stage)) return false;
      if (grade !== '' && !this.courseMatchesGrade(course, grade)) return false;
      return true;
    });
  });

  readonly sortedCourses = computed(() =>
    sortBy(this.filteredCourses(), this.sortKey(), this.sortDir())
  );

  constructor() {
    this.reload();
    this.api.getStages().subscribe({ next: (stages) => this.stages.set(stages) });
    this.api.getGrades().subscribe({ next: (grades) => this.catalogGrades.set(grades) });
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

  setFilterName(value: string): void {
    this.filterName.set(value);
  }

  onFilterStageChange(value: string): void {
    const stage = value === '' ? '' : Number(value);
    this.filterStage.set(stage);
    const grade = this.filterGrade();
    if (grade !== '' && stage !== '' && this.gradeToStage(grade) !== stage) {
      this.filterGrade.set('');
    }
  }

  onFilterGradeChange(value: string): void {
    this.filterGrade.set(value === '' ? '' : Number(value));
  }

  hasActiveFilters(): boolean {
    return !!this.filterName().trim() || this.filterStage() !== '' || this.filterGrade() !== '';
  }

  clearFilters(): void {
    this.filterName.set('');
    this.filterStage.set('');
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

  gradeLabel(grade: number | null | undefined, stageId?: number | null): string {
    return formatCourseAudienceLabel((k, p) => this.locale.t(k, p), grade, stageId);
  }

  stageLabel(stageId: number): string {
    const stage = this.stages().find((s) => s.id === stageId);
    if (stage) return this.locale.lang() === 'ar' ? stage.name : stage.nameEn;
    return formatCourseAudienceLabel((k, p) => this.locale.t(k, p), null, stageId);
  }

  stageOptions(): { value: number; label: string }[] {
    this.locale.lang();
    const stages = this.stages();
    if (stages.length) {
      return stages.map((s) => ({
        value: s.id,
        label: this.locale.lang() === 'ar' ? s.name : s.nameEn
      }));
    }
    return [0, 1, 2, 3].map((id) => ({
      value: id,
      label: formatCourseAudienceLabel((k, p) => this.locale.t(k, p), null, id)
    }));
  }

  gradesForStage(stageId: number | ''): number[] {
    if (stageId === '') return [...this.grades];
    return this.grades.filter((g) => this.gradeToStage(g) === stageId);
  }

  private courseMatchesStage(course: Course, stage: number): boolean {
    if (course.stageId === stage) return true;
    return course.grade != null && this.gradeToStage(course.grade) === stage;
  }

  private courseMatchesGrade(course: Course, grade: number): boolean {
    if (course.grade === grade) return true;
    if (course.grade != null) return false;
    return course.stageId == null || course.stageId === this.gradeToStage(grade);
  }

  gradeToStage(grade: number): number | null {
    if (grade === -1 || grade === 0) return 0;
    if (grade >= 1 && grade <= 6) return 1;
    if (grade >= 7 && grade <= 9) return 2;
    if (grade >= 10 && grade <= 12) return 3;
    return null;
  }

  onCreateStageChange(value: string): void {
    const previousStageId = this.courseStageId;
    this.courseStageId = value === '' ? '' : Number(value);
    this.applyStageToCreateTitle(previousStageId);
    if (this.courseStageId === '') return;
    this.courseGrades = this.courseGrades.filter((g) => this.gradeToStage(g) === this.courseStageId);
  }

  private applyStageToCreateTitle(previousStageId: number | ''): void {
    this.courseTitle = this.composeTitleWithStage(this.courseTitle, previousStageId, this.courseStageId);
  }

  private composeTitleWithStage(
    rawTitle: string,
    previousStageId: number | '',
    stageId: number | ''
  ): string {
    let title = rawTitle.trim();
    if (previousStageId !== '') {
      const previousSuffix = `-${this.stageLabel(previousStageId)}`;
      if (title.endsWith(previousSuffix)) {
        title = title.slice(0, -previousSuffix.length).trim();
      }
    }
    if (stageId === '' || !title) return title;
    const suffix = `-${this.stageLabel(stageId)}`;
    return title.endsWith(suffix) ? title : `${title}${suffix}`;
  }

  onEditStageChange(value: string): void {
    this.editStageId = value === '' ? '' : Number(value);
    if (this.editGrade != null && this.editStageId !== '' && this.gradeToStage(this.editGrade) !== this.editStageId) {
      this.editGrade = null;
    }
  }

  schoolTypeOptions(): { value: CourseSchoolType; label: string }[] {
    this.locale.lang();
    return [
      { value: 'All', label: this.locale.t('common.schoolTypeAll') },
      { value: 'Arabic', label: this.locale.t('common.schoolTypeArabic') },
      { value: 'Language', label: this.locale.t('common.schoolTypeLanguage') }
    ];
  }

  schoolTypeLabel(value?: string | null): string {
    if (!value || value === 'All') return this.locale.t('common.schoolTypeAll');
    if (value === 'Arabic') return this.locale.t('common.schoolTypeArabic');
    if (value === 'Language') return this.locale.t('common.schoolTypeLanguage');
    return value;
  }

  createCourse(): void {
    this.clearStatus();
    this.api
      .createCourse({
        title: this.composeTitleWithStage(this.courseTitle, '', this.courseStageId),
        theme: this.courseTheme,
        description: this.courseDescription,
        ageMin: this.courseAgeMin ?? 8,
        ageMax: this.courseAgeMax ?? 12,
        term: this.courseTerm || null,
        grades: this.courseGrades,
        stageId: this.courseStageId === '' ? null : this.courseStageId,
        schoolType: this.courseSchoolType,
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
          this.courseStageId = '';
          this.courseSchoolType = 'All';
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
    this.editStageId = course.stageId ?? '';
    this.editSchoolType =
      course.schoolType === 'Arabic' || course.schoolType === 'Language' ? course.schoolType : 'All';
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
        stageId: this.editStageId === '' ? null : this.editStageId,
        schoolType: this.editSchoolType,
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
