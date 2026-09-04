import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableMultiSelectComponent } from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { Classroom, Course, CourseLesson, CourseUnit, QuizAttemptReview, TeacherQuizListItem } from '../../models';
import { GRADE_CODES, formatCourseLabel, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { QuestionImageDisplayComponent } from '../../shared/question-image-display/question-image-display.component';
import { QuestionDraftEditorComponent } from '../../shared/question-draft-editor/question-draft-editor.component';
import { QuestionDraft } from '../../shared/question-draft/question-draft.model';
import {
  draftFromGenerated,
  draftFromQuizQuestion,
  emptyQuestionDraft,
  toQuestionPayload,
  validateQuestionDraft
} from '../../shared/question-draft/question-draft.util';
import { paginate, totalPages } from '../../list-query.util';

@Component({
  selector: 'app-teacher-quizzes',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    SearchableMultiSelectComponent,
    FormsModule,
    ReactiveFormsModule,
    IconActionButtonComponent,
    TranslatePipe,
    QuestionDraftEditorComponent,
    QuestionImageDisplayComponent
  ],
  templateUrl: './teacher-quizzes.component.html',
  styleUrls: ['./teacher-panel.css', '../admin/admin-panel.css', './teacher-quizzes.component.css']
})
export class TeacherQuizzesComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly fb = inject(FormBuilder);

  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly quizzes = signal<TeacherQuizListItem[]>([]);
  readonly attempts = signal<QuizAttemptReview[]>([]);
  readonly error = signal('');
  readonly info = signal('');
  readonly grades = GRADE_CODES;

  readonly quizForm = this.fb.nonNullable.group({
    title: ['', Validators.required],
    description: [''],
    courseId: ['', Validators.required],
    unitIds: [[] as string[]],
    lessonIds: [[] as string[]],
    classroomId: [''],
    xp: [30],
    durationMinutes: [0],
    isPublished: [false],
    questionCount: [1]
  });
  questions: QuestionDraft[] = [emptyQuestionDraft()];

  readonly generating = signal(false);
  readonly publishingId = signal<string | null>(null);
  editingQuizId: string | null = null;

  filterFromDate = startOfMonthLocal();
  filterToDate = endOfMonthLocal();
  filterGrade: number | '' = '';
  filterCourseId = '';
  reviewQuizId = '';
  expandedAttemptId = '';

  readonly quizListPageSize = 10;
  readonly attemptsPageSize = 5;
  readonly quizListPage = signal(1);
  readonly attemptsPage = signal(1);

  readonly quizTotalPages = computed(() => totalPages(this.quizzes().length, this.quizListPageSize));
  readonly pagedQuizzes = computed(() =>
    paginate(this.quizzes(), this.quizListPage(), this.quizListPageSize)
  );
  readonly attemptsTotalPages = computed(() => totalPages(this.attempts().length, this.attemptsPageSize));
  readonly pagedAttempts = computed(() =>
    paginate(this.attempts(), this.attemptsPage(), this.attemptsPageSize)
  );

  constructor() {
    this.api.getCourses().subscribe((courses) => {
      this.courses.set(courses);
      if (!this.quizForm.controls.courseId.value && courses[0]) {
        this.quizForm.patchValue({ courseId: courses[0].id });
      }
    });
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.quizForm.controls.classroomId.value && classrooms[0]) {
        this.quizForm.patchValue({ classroomId: classrooms[0].id });
      }
    });
    this.reloadQuizzes();

    this.quizForm.controls.courseId.valueChanges.subscribe(() => this.onCourseChange());
    this.quizForm.controls.unitIds.valueChanges.subscribe(() => this.onUnitsChange());
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade, 'common.allGrades', course.stageId);
  }

  gradeLabel(grade?: number | null): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  dateLabel(value: string): string {
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return value;
    return d.toLocaleString();
  }

  addQuestion(): void {
    this.questions.push(emptyQuestionDraft());
    this.quizForm.patchValue({ questionCount: this.questions.length });
  }

  removeQuestion(index: number): void {
    if (this.questions.length <= 1) return;
    this.questions.splice(index, 1);
    this.quizForm.patchValue({ questionCount: this.questions.length });
  }

  onQuestionCountChange(): void {
    const count = this.clampQuestionCount(this.quizForm.controls.questionCount.value, 1);
    this.quizForm.patchValue({ questionCount: count });
    while (this.questions.length < count) {
      this.questions.push(emptyQuestionDraft());
    }
    if (this.questions.length > count) {
      this.questions = this.questions.slice(0, count);
    }
  }

  resetFilters(): void {
    this.filterFromDate = startOfMonthLocal();
    this.filterToDate = endOfMonthLocal();
    this.filterGrade = '';
    this.filterCourseId = '';
    this.quizListPage.set(1);
    this.reloadQuizzes();
  }

  reloadQuizzes(): void {
    this.error.set('');
    this.api
      .getTeacherQuizzes({
        fromDate: this.filterFromDate || undefined,
        toDate: this.filterToDate || undefined,
        grade: this.filterGrade === '' || this.filterGrade == null ? undefined : Number(this.filterGrade),
        courseId: this.filterCourseId || undefined
      })
      .subscribe({
        next: (quizzes) => {
          this.quizzes.set(quizzes);
          this.quizListPage.set(1);
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.quizzes.loadFailed'))
      });
  }

  goToQuizListPage(page: number): void {
    this.quizListPage.set(Math.min(Math.max(1, page), this.quizTotalPages()));
  }

  goToAttemptsPage(page: number): void {
    this.attemptsPage.set(Math.min(Math.max(1, page), this.attemptsTotalPages()));
    this.expandedAttemptId = '';
  }

  reviewQuiz(quiz: TeacherQuizListItem): void {
    this.reviewQuizId = quiz.id;
    this.expandedAttemptId = '';
    this.attemptsPage.set(1);
    this.error.set('');
    this.api.getQuizAttempts(quiz.id).subscribe({
      next: (attempts) => this.attempts.set(attempts),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.quizzes.loadAttemptsFailed'))
    });
  }

  toggleAttempt(attemptId: string): void {
    this.expandedAttemptId = this.expandedAttemptId === attemptId ? '' : attemptId;
  }

  reviewQuizTitle(): string {
    return this.quizzes().find((q) => q.id === this.reviewQuizId)?.title ?? '';
  }

  onCourseChange(): void {
    this.quizForm.patchValue({ unitIds: [], lessonIds: [] });
  }

  onUnitsChange(): void {
    const allowed = new Set(this.lessonsForUnits().map((lesson) => lesson.id));
    const lessonIds = this.quizForm.controls.lessonIds.value.filter((id) => allowed.has(id));
    this.quizForm.patchValue({ lessonIds });
  }

  unitsForCourse(): CourseUnit[] {
    const courseId = this.quizForm.controls.courseId.value;
    const units = [...(this.courses().find((course) => course.id === courseId)?.units ?? [])];
    return units.sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  lessonsForUnits(): CourseLesson[] {
    const courseId = this.quizForm.controls.courseId.value;
    const unitIds = this.quizForm.controls.unitIds.value;
    const course = this.courses().find((item) => item.id === courseId);
    if (!course || !unitIds.length) return [];
    const selected = new Set(unitIds);
    const lessons = (course.units ?? [])
      .filter((unit) => selected.has(unit.id))
      .flatMap((unit) => unit.lessons ?? []);
    const extra = (course.lessons ?? []).filter((lesson) => lesson.unitId && selected.has(lesson.unitId));
    const byId = new Map<string, CourseLesson>();
    for (const lesson of [...lessons, ...extra]) byId.set(lesson.id, lesson);
    return [...byId.values()].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  generate(): void {
    this.error.set('');
    this.info.set('');
    const { courseId, classroomId, unitIds, lessonIds, questionCount } = this.quizForm.getRawValue();
    if (!courseId) {
      this.error.set(this.locale.t('teacher.ai.needScope'));
      return;
    }

    this.generating.set(true);
    this.api
      .generateAssessment({
        kind: 'Quiz',
        courseId,
        classroomId: classroomId || null,
        unitIds,
        lessonIds,
        questionCount: this.clampQuestionCount(questionCount, 1),
        language: this.locale.lang()
      })
      .subscribe({
        next: (draft) => {
          this.generating.set(false);
          this.questions = draft.questions.length
            ? draft.questions.map((question) => draftFromGenerated(question))
            : [emptyQuestionDraft()];
          this.quizForm.patchValue({
            title: draft.title,
            description: draft.description,
            questionCount: this.questions.length
          });
          this.info.set(this.locale.t('teacher.ai.generated'));
        },
        error: (err) => {
          this.generating.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.ai.generateFailed'));
        }
      });
  }

  createQuiz(): void {
    this.saveQuiz();
  }

  startEdit(quiz: TeacherQuizListItem): void {
    this.error.set('');
    this.info.set('');
    this.editingQuizId = quiz.id;
    this.reviewQuizId = '';
    this.quizForm.patchValue({
      title: quiz.title,
      description: quiz.description,
      courseId: quiz.courseId,
      classroomId: quiz.classroomId || '',
      xp: quiz.xpReward,
      isPublished: quiz.isPublished
    });
    this.api.getTeacherQuiz(quiz.id).subscribe({
      next: (detail) => {
        this.questions = detail.questions.length
          ? detail.questions.map((question) => draftFromQuizQuestion(question))
          : [emptyQuestionDraft()];
        this.quizForm.patchValue({
          title: detail.title,
          description: detail.description,
          courseId: detail.courseId,
          classroomId: detail.classroomId || '',
          xp: detail.xpReward,
          durationMinutes: detail.durationMinutes ?? 0,
          isPublished: detail.isPublished,
          questionCount: this.questions.length
        });
        document.getElementById('quiz-form-section')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.quizzes.loadQuizFailed'))
    });
  }

  cancelEdit(): void {
    this.editingQuizId = null;
    this.resetQuizForm();
    this.error.set('');
    this.info.set('');
  }

  deleteQuiz(quiz: TeacherQuizListItem): void {
    if (!confirm(this.locale.t('teacher.quizzes.confirmDelete', { title: quiz.title }))) {
      return;
    }

    this.error.set('');
    this.info.set('');
    this.api.deleteQuiz(quiz.id).subscribe({
      next: () => {
        if (this.editingQuizId === quiz.id) {
          this.cancelEdit();
        }
        if (this.reviewQuizId === quiz.id) {
          this.reviewQuizId = '';
          this.attempts.set([]);
        }
        this.info.set(this.locale.t('teacher.quizzes.deleted'));
        this.reloadQuizzes();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.quizzes.deleteFailed'))
    });
  }

  publishQuiz(quiz: TeacherQuizListItem): void {
    if (quiz.isPublished || this.publishingId()) {
      return;
    }

    this.error.set('');
    this.info.set('');
    this.publishingId.set(quiz.id);
    this.api.publishQuiz(quiz.id).subscribe({
      next: () => {
        this.publishingId.set(null);
        this.info.set(this.locale.t('teacher.assessments.publishedSuccess'));
        this.reloadQuizzes();
      },
      error: (err) => {
        this.publishingId.set(null);
        this.error.set(this.locale.fromApiError(err, 'teacher.assessments.publishFailed'));
      }
    });
  }

  isPublishing(id: string): boolean {
    return this.publishingId() === id;
  }

  private saveQuiz(): void {
    this.error.set('');
    this.info.set('');

    const title = this.quizForm.controls.title.value.trim();
    if (!title) {
      this.quizForm.controls.title.markAsTouched();
      this.error.set(this.locale.t('teacher.quizzes.titleRequired'));
      return;
    }

    const courseId = this.quizForm.controls.courseId.value.trim();
    if (!courseId) {
      this.quizForm.controls.courseId.markAsTouched();
      this.error.set(this.locale.t('teacher.ai.needScope'));
      return;
    }

    const formValue = this.quizForm.getRawValue();
    const payloads = [];
    for (let index = 0; index < this.questions.length; index++) {
      const errorKey = validateQuestionDraft(this.questions[index], index + 1);
      if (errorKey) {
        this.error.set(this.locale.t(errorKey));
        return;
      }
      payloads.push(toQuestionPayload(this.questions[index], index + 1));
    }

    if (!payloads.length) {
      this.error.set(this.locale.t('teacher.quizzes.promptRequired', { n: 1 }));
      return;
    }

    const payload = {
      courseId,
      classroomId: formValue.classroomId.trim() || null,
      title,
      description: formValue.description.trim() || undefined,
      xpReward: Number(formValue.xp) || 0,
      durationMinutes: Number(formValue.durationMinutes) > 0 ? Number(formValue.durationMinutes) : null,
      isPublished: !!formValue.isPublished,
      questions: payloads
    };

    const editingId = this.editingQuizId;
    const request = editingId
      ? this.api.updateQuiz(editingId, payload)
      : this.api.createQuiz(payload);

    request.subscribe({
      next: () => {
        this.cancelEdit();
        this.info.set(
          this.locale.t(editingId ? 'teacher.quizzes.updated' : 'teacher.quizzes.created')
        );
        this.reloadQuizzes();
      },
      error: (err) =>
        this.error.set(
          this.locale.fromApiError(
            err,
            editingId ? 'teacher.quizzes.updateFailed' : 'teacher.quizzes.createFailed'
          )
        )
    });
  }

  private resetQuizForm(): void {
    const courses = this.courses();
    const classrooms = this.classrooms();
    this.quizForm.reset({
      title: '',
      description: '',
      courseId: courses[0]?.id ?? '',
      unitIds: [],
      lessonIds: [],
      classroomId: classrooms[0]?.id ?? '',
      xp: 30,
      durationMinutes: 0,
      isPublished: false,
      questionCount: 1
    });
    this.questions = [emptyQuestionDraft()];
  }

  private clampQuestionCount(value: number, fallback: number): number {
    const count = Number(value);
    if (!Number.isFinite(count) || count < 1) return fallback;
    return Math.min(12, Math.floor(count));
  }
}

function startOfMonthLocal(): string {
  const d = new Date();
  return toLocalDateString(new Date(d.getFullYear(), d.getMonth(), 1));
}

function endOfMonthLocal(): string {
  const d = new Date();
  return toLocalDateString(new Date(d.getFullYear(), d.getMonth() + 1, 0));
}

function toLocalDateString(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}
