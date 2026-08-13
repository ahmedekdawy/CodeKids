import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Course, CourseLesson, CourseUnit } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-admin-course-tree',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './admin-course-tree.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminCourseTreeComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly courses = signal<Course[]>([]);
  readonly selectedCourseId = signal<string>('');
  readonly expandedUnits = signal<Record<string, boolean>>({});
  readonly message = signal('');
  readonly error = signal('');
  readonly editingUnitId = signal<string | null>(null);
  readonly editingLessonId = signal<string | null>(null);
  readonly addingLessonForUnitId = signal<string | null>(null);

  unitTitle = '';
  unitDescription = '';
  unitSort: number | null = 1;

  editUnitTitle = '';
  editUnitDescription = '';
  editUnitSort: number | null = 1;

  lessonTitle = '';
  lessonTheme = 'General';
  lessonDescription = '';
  lessonDifficulty: number | null = 1;
  lessonXp: number | null = 10;
  lessonSort: number | null = 1;

  editLessonTitle = '';
  editLessonTheme = '';
  editLessonDescription = '';
  editLessonDifficulty: number | null = 1;
  editLessonXp: number | null = 10;
  editLessonSort: number | null = 1;
  editLessonUnitId = '';

  readonly selectedCourse = computed(() =>
    this.courses().find((c) => c.id === this.selectedCourseId()) ?? null
  );

  readonly units = computed(() => {
    const course = this.selectedCourse();
    if (!course) return [] as CourseUnit[];
    if (course.units?.length) {
      return [...course.units].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
    }
    const orphans = (course.lessons ?? []).filter((l) => !l.unitId);
    if (!orphans.length) return [];
    return [
      {
        id: '',
        courseId: course.id,
        title: this.locale.t('admin.courseTree.unassigned'),
        description: '',
        sortOrder: 9999,
        lessons: orphans
      } satisfies CourseUnit
    ];
  });

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getCourses().subscribe({
      next: (courses) => {
        this.courses.set(courses);
        if (!this.selectedCourseId() && courses.length) {
          this.selectedCourseId.set(courses[0].id);
        }
      },
      error: () => this.error.set(this.locale.t('admin.courseTree.loadFailed'))
    });
  }

  onCourseChange(courseId: string): void {
    this.selectedCourseId.set(courseId);
    this.cancelUnitEdit();
    this.cancelLessonEdit();
    this.addingLessonForUnitId.set(null);
    this.message.set('');
    this.error.set('');
  }

  gradeLabel(grade: number | null | undefined): string {
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  courseLabel(course: Course): string {
    return `${course.title} (${this.gradeLabel(course.grade)})`;
  }

  isExpanded(unitId: string): boolean {
    return this.expandedUnits()[unitId] ?? true;
  }

  toggleUnit(unitId: string): void {
    const map = { ...this.expandedUnits() };
    map[unitId] = !(map[unitId] ?? true);
    this.expandedUnits.set(map);
  }

  createUnit(): void {
    const courseId = this.selectedCourseId();
    if (!courseId || !this.unitTitle.trim()) {
      this.error.set(this.locale.t('admin.courseTree.unitTitleRequired'));
      return;
    }
    this.api
      .createCourseUnit(courseId, {
        title: this.unitTitle.trim(),
        description: this.unitDescription.trim() || null,
        sortOrder: this.unitSort ?? 1
      })
      .subscribe({
        next: () => {
          this.unitTitle = '';
          this.unitDescription = '';
          this.unitSort = 1;
          this.message.set(this.locale.t('admin.courseTree.unitCreated'));
          this.error.set('');
          this.reload();
        },
        error: (err) =>
          this.error.set(err?.error?.detail || this.locale.t('admin.courseTree.unitCreateFailed'))
      });
  }

  startUnitEdit(unit: CourseUnit): void {
    if (!unit.id) return;
    this.editingUnitId.set(unit.id);
    this.editUnitTitle = unit.title;
    this.editUnitDescription = unit.description;
    this.editUnitSort = unit.sortOrder;
  }

  cancelUnitEdit(): void {
    this.editingUnitId.set(null);
  }

  saveUnit(): void {
    const unitId = this.editingUnitId();
    if (!unitId || !this.editUnitTitle.trim()) return;
    this.api
      .updateCourseUnit(unitId, {
        title: this.editUnitTitle.trim(),
        description: this.editUnitDescription.trim() || null,
        sortOrder: this.editUnitSort ?? 1
      })
      .subscribe({
        next: () => {
          this.cancelUnitEdit();
          this.message.set(this.locale.t('admin.courseTree.unitUpdated'));
          this.error.set('');
          this.reload();
        },
        error: (err) =>
          this.error.set(err?.error?.detail || this.locale.t('admin.courseTree.unitUpdateFailed'))
      });
  }

  deleteUnit(unit: CourseUnit): void {
    if (!unit.id) return;
    if (!confirm(this.locale.t('admin.courseTree.confirmDeleteUnit', { title: unit.title }))) return;
    this.api.deleteCourseUnit(unit.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.courseTree.unitDeleted'));
        this.error.set('');
        this.reload();
      },
      error: (err) =>
        this.error.set(err?.error?.detail || this.locale.t('admin.courseTree.unitDeleteFailed'))
    });
  }

  startAddLesson(unitId: string): void {
    this.addingLessonForUnitId.set(unitId);
    this.lessonTitle = '';
    this.lessonTheme = 'General';
    this.lessonDescription = '';
    this.lessonDifficulty = 1;
    this.lessonXp = 10;
    this.lessonSort = 1;
  }

  cancelAddLesson(): void {
    this.addingLessonForUnitId.set(null);
  }

  createLesson(): void {
    const unitId = this.addingLessonForUnitId();
    if (!unitId) {
      this.error.set(this.locale.t('admin.courseTree.createUnitFirst'));
      return;
    }
    if (!this.lessonTitle.trim()) {
      this.error.set(this.locale.t('admin.courseTree.lessonTitleRequired'));
      return;
    }
    this.api
      .createCourseLesson(unitId, {
        title: this.lessonTitle.trim(),
        theme: this.lessonTheme.trim() || 'General',
        description: this.lessonDescription.trim() || null,
        difficulty: this.lessonDifficulty ?? 1,
        xpReward: this.lessonXp ?? 10,
        sortOrder: this.lessonSort ?? 1
      })
      .subscribe({
        next: () => {
          this.cancelAddLesson();
          this.message.set(this.locale.t('admin.courseTree.lessonCreated'));
          this.error.set('');
          this.reload();
        },
        error: (err) =>
          this.error.set(err?.error?.detail || this.locale.t('admin.courseTree.lessonCreateFailed'))
      });
  }

  startLessonEdit(lesson: CourseLesson, unitId: string): void {
    this.editingLessonId.set(lesson.id);
    this.editLessonTitle = lesson.title;
    this.editLessonTheme = lesson.theme;
    this.editLessonDescription = lesson.description;
    this.editLessonDifficulty = lesson.difficulty;
    this.editLessonXp = lesson.xpReward;
    this.editLessonSort = lesson.sortOrder;
    this.editLessonUnitId = lesson.unitId || unitId;
  }

  cancelLessonEdit(): void {
    this.editingLessonId.set(null);
  }

  saveLesson(): void {
    const lessonId = this.editingLessonId();
    if (!lessonId || !this.editLessonTitle.trim()) return;
    this.api
      .updateCourseLesson(lessonId, {
        unitId: this.editLessonUnitId || null,
        title: this.editLessonTitle.trim(),
        theme: this.editLessonTheme.trim() || 'General',
        description: this.editLessonDescription.trim() || null,
        difficulty: this.editLessonDifficulty ?? 1,
        xpReward: this.editLessonXp ?? 10,
        sortOrder: this.editLessonSort ?? 1
      })
      .subscribe({
        next: () => {
          this.cancelLessonEdit();
          this.message.set(this.locale.t('admin.courseTree.lessonUpdated'));
          this.error.set('');
          this.reload();
        },
        error: (err) =>
          this.error.set(err?.error?.detail || this.locale.t('admin.courseTree.lessonUpdateFailed'))
      });
  }

  deleteLesson(lesson: CourseLesson): void {
    if (!confirm(this.locale.t('admin.courseTree.confirmDeleteLesson', { title: lesson.title }))) return;
    this.api.deleteCourseLesson(lesson.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.courseTree.lessonDeleted'));
        this.error.set('');
        this.reload();
      },
      error: (err) =>
        this.error.set(err?.error?.detail || this.locale.t('admin.courseTree.lessonDeleteFailed'))
    });
  }
}
