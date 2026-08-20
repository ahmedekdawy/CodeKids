import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { Classroom, Course, QuizAttemptReview, TeacherQuizListItem } from '../../models';
import { GRADE_CODES, formatCourseLabel, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

interface OptionDraft {
  text: string;
}

interface QuestionDraft {
  prompt: string;
  options: OptionDraft[];
  correct: string;
}

function emptyQuestion(): QuestionDraft {
  return { prompt: '', options: [{ text: '' }, { text: '' }], correct: '' };
}

@Component({
  selector: 'app-teacher-quizzes',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
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
  quizCourseId = '';
  quizClassroomId = '';
  questions: QuestionDraft[] = [emptyQuestion()];

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
  }

  removeQuestion(index: number): void {
    if (this.questions.length <= 1) return;
    this.questions.splice(index, 1);
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

  createQuiz(): void {
    this.error.set('');
    this.info.set('');

    const payloads: {
      prompt: string;
      options: string[];
      correctOption: string;
      sortOrder: number;
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
        sortOrder: i + 1
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
          this.reloadQuizzes();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.quizzes.createFailed'))
      });
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
