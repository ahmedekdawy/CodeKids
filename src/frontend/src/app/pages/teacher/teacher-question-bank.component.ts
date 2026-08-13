import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { BankQuestion, BankQuestionType, Course, CourseLesson, CourseUnit } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { MathPromptEditorComponent } from '../../shared/math-prompt-editor/math-prompt-editor.component';
import { SafeHtmlPipe } from '../../shared/safe-html.pipe';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatCourseLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

interface OptionDraft {
  text: string;
}

interface ChildDraft {
  prompt: string;
  questionType: BankQuestionType;
  options: OptionDraft[];
  correctAnswer: string;
  correctKeys: string[];
  points: number;
}

@Component({
  selector: 'app-teacher-question-bank',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, MathPromptEditorComponent, SafeHtmlPipe, IconActionButtonComponent, TranslatePipe],
  templateUrl: './teacher-question-bank.component.html',
  styleUrls: ['./teacher-panel.css', './teacher-question-bank.component.css']
})
export class TeacherQuestionBankComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly courses = signal<Course[]>([]);
  readonly questions = signal<BankQuestion[]>([]);
  readonly error = signal('');
  readonly info = signal('');

  courseId = '';
  unitId = '';
  lessonId = '';
  questionType: BankQuestionType = 'SingleChoice';
  prompt = '';
  passageText = '';
  options: OptionDraft[] = [{ text: '' }, { text: '' }];
  correctAnswer = '';
  correctKeys: string[] = [];
  points = 1;
  children: ChildDraft[] = [];

  readonly types: BankQuestionType[] = [
    'Choose',
    'TrueFalse',
    'SingleChoice',
    'MultiChoice',
    'Paragraph',
    'Underline'
  ];

  constructor() {
    this.api.getCourses().subscribe((courses) => {
      this.courses.set(courses);
      if (!this.courseId && courses[0]) this.courseId = courses[0].id;
      this.reload();
    });
  }

  typeLabel(type: BankQuestionType | string): string {
    const map: Record<string, string> = {
      Choose: 'qtype.choose',
      TrueFalse: 'qtype.trueFalse',
      SingleChoice: 'qtype.singleChoice',
      MultiChoice: 'qtype.multiChoice',
      Paragraph: 'qtype.paragraph',
      Underline: 'qtype.underline'
    };
    return this.locale.t(map[type] ?? type);
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade);
  }

  courseLabelById(courseId: string, fallbackTitle?: string | null): string {
    const course = this.courses().find((c) => c.id === courseId);
    if (course) return this.courseLabel(course);
    return formatCourseLabel((k, p) => this.locale.t(k, p), fallbackTitle, null);
  }

  onCourseChange(): void {
    this.unitId = '';
    this.lessonId = '';
    this.reload();
  }

  onUnitChange(): void {
    this.lessonId = '';
  }

  unitsForCourse(): CourseUnit[] {
    const units = [...(this.courses().find((c) => c.id === this.courseId)?.units ?? [])];
    return units.sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  lessonsForUnit(): CourseLesson[] {
    const course = this.courses().find((c) => c.id === this.courseId);
    if (!course) return [];
    const units = this.unitsForCourse();
    if (!units.length) {
      return [...(course.lessons ?? [])].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
    }
    if (!this.unitId) return [];
    const fromUnit = course.units?.find((u) => u.id === this.unitId)?.lessons;
    const lessons = fromUnit?.length
      ? fromUnit
      : (course.lessons ?? []).filter((l) => l.unitId === this.unitId);
    return [...lessons].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  isParagraph(type: BankQuestionType = this.questionType): boolean {
    return type === 'Paragraph';
  }

  needsOptions(type: BankQuestionType = this.questionType): boolean {
    return type === 'Choose' || type === 'SingleChoice' || type === 'MultiChoice';
  }

  isMulti(type: BankQuestionType = this.questionType): boolean {
    return type === 'MultiChoice';
  }

  optionLabel(index: number): string {
    return String.fromCharCode(65 + index);
  }

  filledOptions(list: OptionDraft[]): { key: string; text: string }[] {
    return list
      .map((o, i) => ({ key: this.optionLabel(i), text: (o.text || '').trim() }))
      .filter((o) => o.text.length > 0);
  }

  onTypeChange(): void {
    if (this.isParagraph() && this.children.length === 0) {
      this.addChild();
    }
    if (!this.isParagraph()) {
      this.children = [];
    }
    if (this.questionType === 'TrueFalse') {
      this.correctAnswer = 'True';
      this.correctKeys = [];
    } else if (this.needsOptions()) {
      if (this.options.length < 2) this.options = [{ text: '' }, { text: '' }];
      this.correctAnswer = '';
      this.correctKeys = [];
    } else {
      this.correctAnswer = '';
      this.correctKeys = [];
    }
  }

  addOption(): void {
    if (this.options.length >= 26) return;
    this.options.push({ text: '' });
  }

  removeOption(index: number): void {
    if (this.options.length <= 2) return;
    this.options.splice(index, 1);
    this.syncCorrectAfterOptionChange();
  }

  syncCorrectAfterOptionChange(): void {
    const keys = new Set(this.filledOptions(this.options).map((o) => o.key));
    if (this.isMulti()) {
      this.correctKeys = this.correctKeys.filter((k) => keys.has(k));
      this.correctAnswer = this.correctKeys.join(',');
    } else if (this.correctAnswer && !keys.has(this.correctAnswer)) {
      this.correctAnswer = '';
    }
  }

  toggleCorrectKey(key: string): void {
    const set = new Set(this.correctKeys);
    if (set.has(key)) set.delete(key);
    else set.add(key);
    this.correctKeys = [...set].sort();
    this.correctAnswer = this.correctKeys.join(',');
  }

  isCorrectKey(key: string): boolean {
    return this.correctKeys.includes(key);
  }

  addChild(): void {
    this.children.push({
      prompt: '',
      questionType: 'SingleChoice',
      options: [{ text: '' }, { text: '' }],
      correctAnswer: '',
      correctKeys: [],
      points: 1
    });
  }

  removeChild(index: number): void {
    this.children.splice(index, 1);
  }

  addChildOption(child: ChildDraft): void {
    if (child.options.length >= 26) return;
    child.options.push({ text: '' });
  }

  removeChildOption(child: ChildDraft, index: number): void {
    if (child.options.length <= 2) return;
    child.options.splice(index, 1);
    const keys = new Set(this.filledOptions(child.options).map((o) => o.key));
    if (child.questionType === 'MultiChoice') {
      child.correctKeys = child.correctKeys.filter((k) => keys.has(k));
      child.correctAnswer = child.correctKeys.join(',');
    } else if (child.correctAnswer && !keys.has(child.correctAnswer)) {
      child.correctAnswer = '';
    }
  }

  toggleChildCorrectKey(child: ChildDraft, key: string): void {
    const set = new Set(child.correctKeys);
    if (set.has(key)) set.delete(key);
    else set.add(key);
    child.correctKeys = [...set].sort();
    child.correctAnswer = child.correctKeys.join(',');
  }

  plainPrompt(html: string): string {
    const el = document.createElement('div');
    el.innerHTML = html || '';
    return (el.textContent || '').trim();
  }

  createQuestion(): void {
    this.error.set('');
    this.info.set('');
    if (!this.courseId || !this.plainPrompt(this.prompt)) {
      this.error.set(this.locale.t('teacher.qbank.required'));
      return;
    }

    if (this.needsOptions()) {
      const filled = this.filledOptions(this.options);
      if (filled.length < 2) {
        this.error.set(this.locale.t('teacher.qbank.minOptions'));
        return;
      }
      if (this.isMulti()) {
        this.correctAnswer = this.correctKeys.join(',');
        if (!this.correctKeys.length) {
          this.error.set(this.locale.t('teacher.qbank.selectMulti'));
          return;
        }
      } else if (!this.correctAnswer) {
        this.error.set(this.locale.t('teacher.qbank.selectSingle'));
        return;
      }
    }

    const optionTexts = this.needsOptions() ? this.filledOptions(this.options).map((o) => o.text) : undefined;

    this.api
      .createBankQuestion({
        courseId: this.courseId,
        lessonId: this.lessonId || null,
        questionType: this.questionType,
        prompt: this.prompt.trim(),
        passageText: this.passageText.trim() || undefined,
        options: optionTexts,
        correctAnswer: this.isParagraph() ? '' : this.correctAnswer,
        points: this.points,
        sortOrder: 1,
        children: this.isParagraph()
          ? this.children.map((c, i) => {
              const childOptions =
                c.questionType === 'TrueFalse'
                  ? undefined
                  : this.filledOptions(c.options).map((o) => o.text);
              const correct =
                c.questionType === 'MultiChoice' ? c.correctKeys.join(',') : c.correctAnswer;
              return {
                prompt: c.prompt,
                questionType: c.questionType,
                options: childOptions,
                correctAnswer: correct,
                points: c.points,
                sortOrder: i + 1
              };
            })
          : undefined
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.qbank.added'));
          this.prompt = '';
          this.passageText = '';
          this.options = [{ text: '' }, { text: '' }];
          this.correctAnswer = this.questionType === 'TrueFalse' ? 'True' : '';
          this.correctKeys = [];
          this.children = [];
          if (this.isParagraph()) this.addChild();
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err,'teacher.qbank.createFailed'))
      });
  }

  deleteQuestion(question: BankQuestion): void {
    if (!confirm(this.locale.t('teacher.qbank.confirmDelete'))) return;
    this.api.deleteBankQuestion(question.id).subscribe({
      next: () => {
        this.info.set(this.locale.t('teacher.qbank.deleted'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err,'teacher.qbank.deleteFailed'))
    });
  }

  private reload(): void {
    this.api.getBankQuestions(this.courseId || undefined).subscribe((questions) => this.questions.set(questions));
  }
}
