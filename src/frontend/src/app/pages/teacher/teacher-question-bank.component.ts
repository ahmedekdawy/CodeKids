import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { BankQuestion, BankQuestionType, Course, CourseLesson, CourseUnit } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { SafeHtmlPipe } from '../../shared/safe-html.pipe';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatCourseLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { QuestionDraftEditorComponent } from '../../shared/question-draft-editor/question-draft-editor.component';
import { QuestionDraft } from '../../shared/question-draft/question-draft.model';
import {
  emptyQuestionDraft,
  plainPrompt,
  questionTypeLabelKey,
  toQuestionPayload,
  validateQuestionDraft
} from '../../shared/question-draft/question-draft.util';
import { QuestionImageDisplayComponent } from '../../shared/question-image-display/question-image-display.component';

@Component({
  selector: 'app-teacher-question-bank',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, SafeHtmlPipe, IconActionButtonComponent, TranslatePipe, QuestionImageDisplayComponent, QuestionDraftEditorComponent],
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
  draft: QuestionDraft = emptyQuestionDraft();

  constructor() {
    this.api.getCourses().subscribe((courses) => {
      this.courses.set(courses);
      if (!this.courseId && courses[0]) this.courseId = courses[0].id;
      this.reload();
    });
  }

  typeLabel(type: BankQuestionType | string): string {
    return this.locale.t(questionTypeLabelKey(type));
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade, 'common.allGrades', course.stageId);
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

  createQuestion(): void {
    this.error.set('');
    this.info.set('');
    if (!this.courseId || !plainPrompt(this.draft.prompt)) {
      this.error.set(this.locale.t('teacher.qbank.required'));
      return;
    }

    const errorKey = validateQuestionDraft(this.draft);
    if (errorKey) {
      this.error.set(this.locale.t(errorKey));
      return;
    }

    const payload = toQuestionPayload(this.draft, 1);
    this.api
      .createBankQuestion({
        courseId: this.courseId,
        lessonId: this.lessonId || null,
        questionType: payload.questionType,
        prompt: payload.prompt,
        passageText: payload.passageText,
        options: payload.options,
        correctAnswer: payload.correctAnswer,
        points: payload.points,
        sortOrder: 1,
        promptImageMediaAssetId: payload.promptImageMediaAssetId,
        children: payload.children?.map((child) => ({
          prompt: child.prompt,
          questionType: child.questionType,
          options: child.options,
          correctAnswer: child.correctAnswer,
          points: child.points,
          sortOrder: child.sortOrder,
          promptImageMediaAssetId: child.promptImageMediaAssetId
        }))
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.qbank.added'));
          this.draft = emptyQuestionDraft(this.draft.questionType);
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
