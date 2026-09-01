import { Component, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableMultiSelectComponent } from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { Classroom, Course, CourseLesson, CourseUnit, QuizAttemptReview, TeacherQuizListItem, TeacherQuizQuestionDetail } from '../../models';
import { GRADE_CODES, formatCourseLabel, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { QuestionImageUploadComponent } from '../../shared/question-image-upload/question-image-upload.component';
import { QuestionImageDisplayComponent } from '../../shared/question-image-display/question-image-display.component';
import { paginate, totalPages } from '../../list-query.util';

interface OptionDraft {
  key: string;
  text: string;
}

interface QuestionDraft {
  key: string;
  id?: string;
  prompt: string;
  options: OptionDraft[];
  correct: string;
  promptImageMediaAssetId?: string | null;
  promptImageUrl?: string | null;
}

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
    QuestionImageUploadComponent,
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
    questionCount: [1],
    questions: this.fb.array([this.createQuestionGroup()])
  });

  readonly generating = signal(false);
  readonly questionRenderKey = signal(0);
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

  get questionsArray(): FormArray<FormGroup> {
    return this.quizForm.controls.questions;
  }

  questionGroup(index: number): FormGroup {
    return this.questionsArray.at(index);
  }

  optionsArray(questionIndex: number): FormArray<FormGroup> {
    return this.questionGroup(questionIndex).controls['options'] as FormArray<FormGroup>;
  }

  questionTrackId(index: number): string {
    return String(this.questionGroup(index).controls['key'].value ?? index);
  }

  questionRenderTrackId(index: number): string {
    return `${this.questionRenderKey()}-${this.questionTrackId(index)}`;
  }

  optionTrackId(questionIndex: number, optionIndex: number): string {
    return String(this.optionsArray(questionIndex).at(optionIndex).controls['key'].value ?? optionIndex);
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

  optionLabel(index: number): string {
    return String.fromCharCode(65 + index);
  }

  filledOptions(questionIndex: number): { key: string; text: string }[] {
    return this.optionsArray(questionIndex).controls
      .map((control, index) => ({
        key: this.optionLabel(index),
        text: String(control.controls['text'].value ?? '').trim()
      }))
      .filter((option) => option.text.length > 0);
  }

  addQuestion(): void {
    this.questionsArray.push(this.createQuestionGroup());
    this.quizForm.patchValue({ questionCount: this.questionsArray.length });
  }

  removeQuestion(index: number): void {
    if (this.questionsArray.length <= 1) return;
    this.questionsArray.removeAt(index);
    this.quizForm.patchValue({ questionCount: this.questionsArray.length });
  }

  onQuestionCountChange(): void {
    const count = this.clampQuestionCount(this.quizForm.controls.questionCount.value, 1);
    this.quizForm.patchValue({ questionCount: count });
    while (this.questionsArray.length < count) {
      this.questionsArray.push(this.createQuestionGroup());
    }
    while (this.questionsArray.length > count) {
      this.questionsArray.removeAt(this.questionsArray.length - 1);
    }
  }

  addOption(questionIndex: number): void {
    const options = this.optionsArray(questionIndex);
    if (options.length >= 26) return;
    options.push(this.createOptionGroup());
  }

  removeOption(questionIndex: number, optionIndex: number): void {
    const options = this.optionsArray(questionIndex);
    if (options.length <= 2) return;
    options.removeAt(optionIndex);
    this.onOptionTextChange(questionIndex);
  }

  onOptionTextChange(questionIndex: number): void {
    const correct = String(this.questionGroup(questionIndex).controls['correct'].value ?? '');
    const keys = new Set(this.filledOptions(questionIndex).map((option) => option.key));
    if (correct && !keys.has(correct)) {
      this.questionGroup(questionIndex).patchValue({ correct: '' });
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
          this.setQuestions(
            draft.questions.length
              ? draft.questions.map((question) => ({
                  key: crypto.randomUUID(),
                  prompt: question.prompt,
                  options: (question.options?.length ? question.options : ['', '', '']).map((text) => ({
                    key: crypto.randomUUID(),
                    text
                  })),
                  correct: question.correctOption || ''
                }))
              : [this.emptyQuestionDraft()]
          );
          this.quizForm.patchValue({
            title: draft.title,
            description: draft.description,
            questionCount: this.questionsArray.length
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
      xp: quiz.xpReward
    });
    this.api.getTeacherQuiz(quiz.id).subscribe({
      next: (detail) => {
        this.setQuestions(
          detail.questions.length
            ? detail.questions.map((question) => this.mapDetailQuestion(question))
            : [this.emptyQuestionDraft()]
        );
        this.quizForm.patchValue({
          title: detail.title,
          description: detail.description,
          courseId: detail.courseId,
          classroomId: detail.classroomId || '',
          xp: detail.xpReward,
          questionCount: this.questionsArray.length
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
    const payloads: {
      id?: string | null;
      prompt: string;
      options: string[];
      correctOption: string;
      sortOrder: number;
      promptImageMediaAssetId?: string | null;
    }[] = [];

    for (let index = 0; index < this.questionsArray.length; index++) {
      const question = this.questionGroup(index).getRawValue() as {
        id: string;
        prompt: string;
        correct: string;
        promptImageMediaAssetId: string | null;
      };
      const prompt = (question.prompt || '').trim();
      if (!prompt) {
        this.error.set(this.locale.t('teacher.quizzes.promptRequired', { n: index + 1 }));
        return;
      }

      const filled = this.filledOptions(index);
      if (filled.length < 2) {
        this.error.set(this.locale.t('teacher.quizzes.minOptionsForQuestion', { n: index + 1 }));
        return;
      }
      if (!question.correct) {
        this.error.set(this.locale.t('teacher.quizzes.selectCorrectForQuestion', { n: index + 1 }));
        return;
      }

      payloads.push({
        ...(question.id ? { id: question.id } : {}),
        prompt,
        options: filled.map((option) => option.text),
        correctOption: question.correct,
        sortOrder: index + 1,
        promptImageMediaAssetId: question.promptImageMediaAssetId || null
      });
    }

    const payload = {
      courseId,
      classroomId: formValue.classroomId.trim() || null,
      title,
      description: formValue.description.trim() || undefined,
      xpReward: Number(formValue.xp) || 0,
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

  private createQuestionGroup(draft?: QuestionDraft): FormGroup {
    const options = draft?.options?.length
      ? draft.options.map((option) => this.createOptionGroup(option.text, option.key))
      : [this.createOptionGroup(), this.createOptionGroup(), this.createOptionGroup()];

    return this.fb.group({
      key: [draft?.key ?? crypto.randomUUID()],
      id: [draft?.id ?? ''],
      prompt: [draft?.prompt ?? ''],
      options: this.fb.array(options),
      correct: [draft?.correct ?? ''],
      promptImageMediaAssetId: [draft?.promptImageMediaAssetId ?? null],
      promptImageUrl: [draft?.promptImageUrl ?? null]
    });
  }

  private createOptionGroup(text = '', key: string = crypto.randomUUID()): FormGroup {
    return this.fb.group({
      key: [key],
      text: [text]
    });
  }

  private emptyQuestionDraft(): QuestionDraft {
    return {
      key: crypto.randomUUID(),
      prompt: '',
      options: [
        { key: crypto.randomUUID(), text: '' },
        { key: crypto.randomUUID(), text: '' },
        { key: crypto.randomUUID(), text: '' }
      ],
      correct: ''
    };
  }

  private setQuestions(questions: QuestionDraft[]): void {
    this.quizForm.setControl(
      'questions',
      this.fb.array(questions.map((question) => this.createQuestionGroup(question)))
    );
    this.quizForm.patchValue({ questionCount: questions.length });
    this.questionRenderKey.update((value) => value + 1);
  }

  private mapDetailQuestion(question: TeacherQuizQuestionDetail): QuestionDraft {
    const raw = question as TeacherQuizQuestionDetail & Record<string, unknown>;
    const prompt = String(raw.prompt ?? raw['Prompt'] ?? '');
    const correctOption = String(raw.correctOption ?? raw['CorrectOption'] ?? '').toUpperCase();
    const optionsRaw = (raw.options ?? raw['Options']) as Array<{ key?: string; text?: string; Key?: string; Text?: string }> | undefined;
    const options = optionsRaw?.length
      ? optionsRaw.map((option) => ({
          key: crypto.randomUUID(),
          text: String(option.text ?? option.Text ?? '')
        }))
      : [
          { key: crypto.randomUUID(), text: '' },
          { key: crypto.randomUUID(), text: '' },
          { key: crypto.randomUUID(), text: '' }
        ];

    return {
      key: String(raw.id ?? raw['Id'] ?? crypto.randomUUID()),
      id: String(raw.id ?? raw['Id'] ?? ''),
      prompt,
      options,
      correct: correctOption,
      promptImageMediaAssetId: (raw.promptImageMediaAssetId ?? raw['PromptImageMediaAssetId'] ?? null) as string | null,
      promptImageUrl: (raw.promptImageUrl ?? raw['PromptImageUrl'] ?? null) as string | null
    };
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
      questionCount: 1
    });
    this.setQuestions([this.emptyQuestionDraft()]);
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
