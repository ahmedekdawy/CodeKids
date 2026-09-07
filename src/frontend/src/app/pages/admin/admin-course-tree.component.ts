import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Course, CourseLesson, CourseUnit, CourseVideoSummary, GeneratedCourseTree, Grade, Stage } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import {
  GRADE_CODES,
  formatCourseAudienceLabel,
  formatCourseLabel,
  formatGradeLabel,
  gradeToStage
} from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-admin-course-tree',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe, RouterLink],
  templateUrl: './admin-course-tree.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminCourseTreeComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly auth = inject(AuthService);

  readonly courses = signal<Course[]>([]);
  readonly selectedCourse = signal<Course | null>(null);
  readonly selectedCourseId = signal<string>('');
  readonly expandedUnits = signal<Record<string, boolean>>({});
  readonly message = signal('');
  readonly error = signal('');
  readonly editingUnitId = signal<string | null>(null);
  readonly editingLessonId = signal<string | null>(null);
  readonly addingLessonForUnitId = signal<string | null>(null);
  readonly filterStage = signal<number | ''>('');
  readonly filterGrade = signal<number | ''>('');
  readonly stages = signal<Stage[]>([]);
  readonly catalogGrades = signal<Grade[]>([]);
  readonly generating = signal(false);
  readonly applying = signal(false);
  readonly aiDraft = signal<GeneratedCourseTree | null>(null);
  readonly isSuperAdmin = computed(() => this.auth.user()?.role === 'SuperAdmin');
  readonly emptyCoursesKey = computed(() =>
    this.isSuperAdmin() ? 'admin.courseTree.noCourses' : 'admin.courseTree.noAssignedCourses'
  );

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

  aiMode: 'rebuild' | 'update' = 'update';
  aiPrompt = '';

  readonly aiDraftUnitCount = computed(() => this.aiDraft()?.units.length ?? 0);
  readonly aiDraftLessonCount = computed(() =>
    (this.aiDraft()?.units ?? []).reduce((sum, unit) => sum + unit.lessons.length, 0)
  );

  readonly filteredCourses = computed(() => {
    const stage = this.filterStage();
    const grade = this.filterGrade();
    return this.courses().filter((course) => {
      if (stage !== '' && !this.courseMatchesStage(course, stage)) return false;
      if (grade !== '' && !this.courseMatchesGrade(course, grade)) return false;
      return true;
    });
  });

  readonly courseOptions = computed(() => {
    this.locale.lang();
    return this.filteredCourses().map((course) => ({
      value: course.id,
      label: this.courseLabel(course)
    }));
  });

  readonly filterGradeOptions = computed(() => {
    this.locale.lang();
    const stageId = this.filterStage();
    const catalog = this.catalogGrades().filter((g) => stageId === '' || g.stageId === stageId);
    if (!catalog.length) {
      const grades = stageId === '' ? [...GRADE_CODES] : GRADE_CODES.filter((g) => gradeToStage(g) === stageId);
      return grades.map((g) => ({
        value: g,
        label: formatGradeLabel((k, p) => this.locale.t(k, p), g)
      }));
    }
    return catalog.map((g) => ({
      value: g.id,
      label: this.locale.lang() === 'ar' ? g.name : g.nameEn
    }));
  });

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
    this.api.getStages().subscribe({ next: (stages) => this.stages.set(stages) });
    this.api.getGrades().subscribe({ next: (grades) => this.catalogGrades.set(grades) });
  }

  reload(): void {
    this.api.getCourses(false).subscribe({
      next: (courses) => {
        this.courses.set(courses);
        const selectedId = this.selectedCourseId();
        const visible = this.filteredCourses();
        if (selectedId && visible.some((c) => c.id === selectedId)) {
          this.reloadTree();
          return;
        }
        const nextId = visible[0]?.id ?? courses[0]?.id ?? '';
        this.selectedCourseId.set(nextId);
        this.reloadTree();
      },
      error: () => this.error.set(this.locale.t('admin.courseTree.loadFailed'))
    });
  }

  reloadTree(): void {
    const courseId = this.selectedCourseId();
    if (!courseId) {
      this.selectedCourse.set(null);
      return;
    }
    this.api.getCourse(courseId).subscribe({
      next: (course) => {
        if (this.selectedCourseId() === course.id) {
          this.selectedCourse.set(course);
        }
      },
      error: () => this.error.set(this.locale.t('admin.courseTree.loadFailed'))
    });
  }

  onCourseChange(courseId: string): void {
    this.selectedCourseId.set(courseId);
    this.selectedCourse.set(null);
    this.cancelUnitEdit();
    this.cancelLessonEdit();
    this.addingLessonForUnitId.set(null);
    this.aiDraft.set(null);
    this.message.set('');
    this.error.set('');
    this.reloadTree();
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

  onFilterStageChange(value: string): void {
    const stage = value === '' ? '' : Number(value);
    this.filterStage.set(stage);
    const grade = this.filterGrade();
    if (grade !== '' && stage !== '' && gradeToStage(grade) !== stage) {
      this.filterGrade.set('');
    }
    this.ensureSelectedVisible();
  }

  onFilterGradeChange(value: string): void {
    this.filterGrade.set(value === '' ? '' : Number(value));
    this.ensureSelectedVisible();
  }

  hasActiveFilters(): boolean {
    return this.filterStage() !== '' || this.filterGrade() !== '';
  }

  clearFilters(): void {
    this.filterStage.set('');
    this.filterGrade.set('');
    this.ensureSelectedVisible();
  }

  courseLabel(course: Course): string {
    const base = formatCourseLabel(
      (k, p) => this.locale.t(k, p),
      course.title,
      course.grade,
      'common.allGrades',
      course.stageId
    );
    const track = (course.trackName || '').trim();
    return track ? `${base} — ${track}` : base;
  }

  private ensureSelectedVisible(): void {
    const id = this.selectedCourseId();
    const visible = this.filteredCourses();
    if (id && visible.some((c) => c.id === id)) return;
    const nextId = visible[0]?.id ?? '';
    this.selectedCourseId.set(nextId);
    this.reloadTree();
  }

  private courseMatchesStage(course: Course, stage: number): boolean {
    if (course.stageId === stage) return true;
    return course.grade != null && gradeToStage(course.grade) === stage;
  }

  private courseMatchesGrade(course: Course, grade: number): boolean {
    if (course.grade === grade) return true;
    if (course.grade != null) return false;
    return course.stageId == null || course.stageId === gradeToStage(grade);
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
          this.reloadTree();
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
          this.reloadTree();
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
          this.reloadTree();
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
          this.reloadTree();
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

  toggleAsk(scope: 'course' | 'unit' | 'lesson', id: string, enabled: boolean): void {
    this.api.setStudentAskEnabled(scope, id, enabled).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.courseTree.studentAskUpdated'));
        this.error.set('');
        this.reloadTree();
      },
      error: (err) => this.error.set(err?.error?.detail || this.locale.t('admin.courseTree.studentAskFailed'))
    });
  }

  courseVideos(course: Course): CourseVideoSummary[] {
    return course.videos ?? [];
  }

  generateAiPreview(): void {
    const courseId = this.selectedCourseId();
    if (!courseId || this.generating()) return;
    this.generating.set(true);
    this.error.set('');
    this.api
      .generateCourseTree(courseId, {
        mode: this.aiMode,
        prompt: this.aiPrompt.trim() || undefined,
        language: this.locale.lang(),
        apply: false
      })
      .subscribe({
        next: (draft) => {
          this.aiDraft.set(draft);
          this.message.set(this.locale.t('admin.courseTree.aiGenerated'));
          this.generating.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.detail || this.locale.t('admin.courseTree.aiGenerateFailed'));
          this.generating.set(false);
        }
      });
  }

  applyAiDraft(): void {
    const courseId = this.selectedCourseId();
    const draft = this.aiDraft();
    if (!courseId || !draft || this.applying()) return;
    if (this.aiMode === 'rebuild' && !confirm(this.locale.t('admin.courseTree.confirmRebuild'))) {
      return;
    }
    this.applying.set(true);
    this.error.set('');
    this.api
      .generateCourseTree(courseId, {
        mode: this.aiMode,
        prompt: this.aiPrompt.trim() || undefined,
        language: this.locale.lang(),
        apply: true
      })
      .subscribe({
        next: (result) => {
          this.aiDraft.set(result);
          this.message.set(this.locale.t('admin.courseTree.aiApplied'));
          this.applying.set(false);
          this.reload();
        },
        error: (err) => {
          this.error.set(err?.error?.detail || this.locale.t('admin.courseTree.aiApplyFailed'));
          this.applying.set(false);
        }
      });
  }
}
