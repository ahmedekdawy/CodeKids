import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableMultiSelectComponent } from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { Classroom, Course, CourseLesson, CourseUnit, QuizAttemptReview, TeacherQuizListItem } from '../../models';
import { GRADE_CODES, formatCourseLabel, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { QuestionImageUploadComponent } from '../../shared/question-image-upload/question-image-upload.component';

interface OptionDraft {
  text: string;
}

interface QuestionDraft {
  prompt: string;
  options: OptionDraft[];
  correct: string;
  promptImageMediaAssetId?: string | null;
  promptImageUrl?: string | null;
}

function emptyQuestion(): QuestionDraft {
  return { prompt: '', options: [{ text: '' }, { text: '' }], correct: '' };
}

@Component({
  selector: 'app-teacher-quizzes',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    SearchableMultiSelectComponent,
    FormsModule,
    IconActionButtonComponent,
    TranslatePipe,
    QuestionImageUploadComponent
  ],
  templateUrl: './teacher-quizzes.component.html',
  styleUrls: ['./teacher-panel.css', '../admin/admin-panel.css', './teacher-quizzes.component.css']
})
export class TeacherQuizzesComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly quizzes = signal<TeacherQuizListItem[]>([]);
  readonly attempts = signal<QuizAttemptReview[]>([]);
  readonly error = signal('');
  readonly info = signal('');
  readonly grades = GRADE_CODES;

  quizTitle = '';
  quizDescription = '';
  quizXp = 30;
  quizQuestionCount = 5;
  quizCourseId = '';
  quizUnitIds: string[] = [];
  quizLessonIds: string[] = [];
  quizClassroomId = '';
  questions: QuestionDraft[] = [emptyQuestion()];
  readonly generating = signal(false);

  filterFromDate = startOfMonthLocal();
  filterToDate = endOfMonthLocal();
  filterGrade: number | '' = '';
  filterCourseId = '';
  reviewQuizId = '';
  expandedAttemptId = '';

  constructor() {
    this.api.getCourses().subscribe((courses) => {
      this.courses.set(courses);
      if (!this.quizCourseId && courses[0]) this.quizCourseId = courses[0].id;
    });
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.quizClassroomId && classrooms[0]) this.quizClassroomId = classrooms[0].id;
    });
    this.reloadQuizzes();
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

  filledOptions(question: QuestionDraft): { key: string; text: string }[] {
    return question.options
      .map((o, i) => ({ key: this.optionLabel(i), text: (o.text || '').trim() }))
      .filter((o) => o.text.length > 0);
  }

  addQuestion(): void {
    this.questions.push(emptyQuestion());
    this.quizQuestionCount = this.questions.length;
  }

  removeQuestion(index: number): void {
    if (this.questions.length <= 1) return;
    this.questions.splice(index, 1);
    this.quizQuestionCount = this.questions.length;
  }

  onQuestionCountChange(): void {
    const count = this.clampQuestionCount(this.quizQuestionCount, 5);
    this.quizQuestionCount = count;
    while (this.questions.length < count) this.questions.push(emptyQuestion());
    if (this.questions.length > count) this.questions = this.questions.slice(0, count);
  }

  addOption(questionIndex: number): void {
    const question = this.questions[questionIndex];
    if (!question || question.options.length >= 26) return;
    question.options.push({ text: '' });
  }

  removeOption(questionIndex: number, optionIndex: number): void {
    const question = this.questions[questionIndex];
    if (!question || question.options.length <= 2) return;
    question.options.splice(optionIndex, 1);
    this.onOptionTextChange(question);
  }

  onOptionTextChange(question: QuestionDraft): void {
    const keys = new Set(this.filledOptions(question).map((o) => o.key));
    if (question.correct && !keys.has(question.correct)) question.correct = '';
  }

  resetFilters(): void {
    this.filterFromDate = startOfMonthLocal();
    this.filterToDate = endOfMonthLocal();
    this.filterGrade = '';
    this.filterCourseId = '';
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
        next: (quizzes) => this.quizzes.set(quizzes),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.quizzes.loadFailed'))
      });
  }

  reviewQuiz(quiz: TeacherQuizListItem): void {
    this.reviewQuizId = quiz.id;
    this.expandedAttemptId = '';
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
    this.quizUnitIds = [];
    this.quizLessonIds = [];
  }

  onUnitsChange(): void {
    const allowed = new Set(this.lessonsForUnits().map((l) => l.id));
    this.quizLessonIds = this.quizLessonIds.filter((id) => allowed.has(id));
  }

  unitsForCourse(): CourseUnit[] {
    const units = [...(this.courses().find((c) => c.id === this.quizCourseId)?.units ?? [])];
    return units.sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  lessonsForUnits(): CourseLesson[] {
    const course = this.courses().find((c) => c.id === this.quizCourseId);
    if (!course || !this.quizUnitIds.length) return [];
    const selected = new Set(this.quizUnitIds);
    const lessons = (course.units ?? [])
      .filter((u) => selected.has(u.id))
      .flatMap((u) => u.lessons ?? []);
    const extra = (course.lessons ?? []).filter((l) => l.unitId && selected.has(l.unitId));
    const byId = new Map<string, CourseLesson>();
    for (const lesson of [...lessons, ...extra]) byId.set(lesson.id, lesson);
    return [...byId.values()].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  generate(): void {
    this.error.set('');
    this.info.set('');
    if (!this.quizCourseId) {
      this.error.set(this.locale.t('teacher.ai.needScope'));
      return;
    }

    this.generating.set(true);
    this.api
      .generateAssessment({
        kind: 'Quiz',
        courseId: this.quizCourseId,
        classroomId: this.quizClassroomId || null,
        unitIds: this.quizUnitIds,
        lessonIds: this.quizLessonIds,
        questionCount: this.clampQuestionCount(this.quizQuestionCount, 5),
        language: this.locale.lang()
      })
      .subscribe({
        next: (draft) => {
          this.generating.set(false);
          this.quizTitle = draft.title;
          this.quizDescription = draft.description;
          this.questions = draft.questions.length
            ? draft.questions.map((question) => ({
                prompt: question.prompt,
                options: (question.options?.length ? question.options : ['', '']).map((text) => ({ text })),
                correct: question.correctOption || ''
              }))
            : [emptyQuestion()];
          this.quizQuestionCount = this.questions.length;
          this.info.set(this.locale.t('teacher.ai.generated'));
        },
        error: (err) => {
          this.generating.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.ai.generateFailed'));
        }
      });
  }

  createQuiz(): void {
    this.error.set('');
    this.info.set('');

    if (!this.quizCourseId) {
      this.error.set(this.locale.t('teacher.ai.needScope'));
      return;
    }

    const payloads: {
      prompt: string;
      options: string[];
      correctOption: string;
      sortOrder: number;
      promptImageMediaAssetId?: string | null;
    }[] = [];

    for (let i = 0; i < this.questions.length; i++) {
      const question = this.questions[i];
      const prompt = (question.prompt || '').trim();
      if (!prompt) {
        this.error.set(this.locale.t('teacher.quizzes.promptRequired', { n: i + 1 }));
        return;
      }

      const filled = this.filledOptions(question);
      if (filled.length < 2) {
        this.error.set(this.locale.t('teacher.quizzes.minOptionsForQuestion', { n: i + 1 }));
        return;
      }
      if (!question.correct) {
        this.error.set(this.locale.t('teacher.quizzes.selectCorrectForQuestion', { n: i + 1 }));
        return;
      }

      payloads.push({
        prompt,
        options: filled.map((o) => o.text),
        correctOption: question.correct,
        sortOrder: i + 1,
        promptImageMediaAssetId: question.promptImageMediaAssetId || null
      });
    }

    this.api
      .createQuiz({
        courseId: this.quizCourseId,
        classroomId: this.quizClassroomId || null,
        title: this.quizTitle,
        description: this.quizDescription,
        xpReward: this.quizXp,
        questions: payloads
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.quizzes.created'));
          this.quizTitle = '';
          this.quizDescription = '';
          this.questions = [emptyQuestion()];
          this.quizQuestionCount = 1;
          this.reloadQuizzes();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.quizzes.createFailed'))
      });
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
