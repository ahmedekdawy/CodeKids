import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { ChoiceOption, Exam, ExamAttempt, ExamQuestion } from '../../models';
import { SafeHtmlPipe } from '../../shared/safe-html.pipe';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { QuestionImageDisplayComponent } from '../../shared/question-image-display/question-image-display.component';

@Component({
  selector: 'app-exam-play',
  imports: [PageFeedbackComponent, FormsModule, RouterLink, SafeHtmlPipe, TranslatePipe, SiteBrandComponent, ApiBusyIndicatorComponent, QuestionImageDisplayComponent],
  templateUrl: './exam-play.component.html',
  styleUrl: './exam-play.component.css'
})
export class ExamPlayComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly locale = inject(LocaleService);

  readonly exam = signal<Exam | null>(null);
  readonly result = signal<ExamAttempt | null>(null);
  readonly error = signal('');
  readonly answers = signal<Record<string, string>>({});
  readonly multiAnswers = signal<Record<string, Set<string>>>({});

  constructor() {
    const id = this.route.snapshot.paramMap.get('examId');
    if (!id) return;
    this.api.getExam(id).subscribe({
      next: (exam) => {
        this.exam.set(exam);
        const seed: Record<string, string> = {};
        const multi: Record<string, Set<string>> = {};
        for (const q of this.flatten(exam.questions)) {
          seed[q.id] = '';
          if (q.questionType === 'MultiChoice') multi[q.id] = new Set();
        }
        this.answers.set(seed);
        this.multiAnswers.set(multi);
        this.api.startExam(exam.id).subscribe({
          error: (err) => {
            if (!this.locale.hasApiErrorCode(err, 'api.errors.exam.alreadySubmitted')) {
              this.error.set(this.locale.fromApiError(err, 'play.examStartFailed'));
            }
          }
        });
      },
      error: () => this.error.set(this.locale.t('play.examNotFound'))
    });
  }

  choiceOptions(question: ExamQuestion): ChoiceOption[] {
    if (question.options?.length) return question.options;
    const legacy: ChoiceOption[] = [];
    if (question.optionA) legacy.push({ key: 'A', text: question.optionA });
    if (question.optionB) legacy.push({ key: 'B', text: question.optionB });
    if (question.optionC) legacy.push({ key: 'C', text: question.optionC });
    if (question.optionD) legacy.push({ key: 'D', text: question.optionD });
    return legacy;
  }

  flatten(questions: ExamQuestion[]): ExamQuestion[] {
    const list: ExamQuestion[] = [];
    for (const q of questions) {
      list.push(q);
      list.push(...this.flatten(q.children || []));
    }
    return list;
  }

  setAnswer(questionId: string, value: string): void {
    this.answers.update((current) => ({ ...current, [questionId]: value }));
  }

  toggleMulti(questionId: string, key: string): void {
    const current = this.multiAnswers();
    const set = new Set(current[questionId] || []);
    if (set.has(key)) set.delete(key);
    else set.add(key);
    this.multiAnswers.set({ ...current, [questionId]: set });
    this.setAnswer(questionId, [...set].sort().join(','));
  }

  isMultiChecked(questionId: string, key: string): boolean {
    return this.multiAnswers()[questionId]?.has(key) === true;
  }

  submit(): void {
    const exam = this.exam();
    if (!exam) return;
    const answerable = this.flatten(exam.questions).filter((q) => q.questionType !== 'Paragraph');
    this.api
      .submitExam({
        examId: exam.id,
        answers: answerable.map((q) => ({
          questionId: q.id,
          answerText: this.answers()[q.id] || ''
        }))
      })
      .subscribe({
        next: (result) => this.result.set(result),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'play.submitExamFailed'))
      });
  }
}
