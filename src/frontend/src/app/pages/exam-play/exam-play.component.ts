import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Exam, ExamAttempt } from '../../models';
import { SafeHtmlPipe } from '../../shared/safe-html.pipe';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { QuestionImageDisplayComponent } from '../../shared/question-image-display/question-image-display.component';
import { QuestionPlayPromptComponent } from '../../shared/question-play-prompt/question-play-prompt.component';
import { AnswerImageDraft } from '../../shared/question-play-prompt/playable-question';
import { answerableQuestions, flattenQuestions } from '../../shared/question-draft/question-draft.util';

@Component({
  selector: 'app-exam-play',
  imports: [PageFeedbackComponent, FormsModule, RouterLink, SafeHtmlPipe, TranslatePipe, SiteBrandComponent, ApiBusyIndicatorComponent, QuestionImageDisplayComponent, QuestionPlayPromptComponent],
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
  readonly answerImages = signal<Record<string, AnswerImageDraft>>({});

  constructor() {
    const id = this.route.snapshot.paramMap.get('examId');
    if (!id) return;
    this.api.getExam(id).subscribe({
      next: (exam) => {
        this.exam.set(exam);
        const seed: Record<string, string> = {};
        const multi: Record<string, Set<string>> = {};
        const images: Record<string, AnswerImageDraft> = {};
        for (const q of flattenQuestions(exam.questions)) {
          seed[q.id] = '';
          if (q.questionType === 'MultiChoice') multi[q.id] = new Set();
          images[q.id] = { mediaAssetId: null, imageUrl: null };
        }
        this.answers.set(seed);
        this.multiAnswers.set(multi);
        this.answerImages.set(images);
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

  setAnswerImage(questionId: string, mediaAssetId: string | null, imageUrl: string | null): void {
    this.answerImages.update((current) => ({
      ...current,
      [questionId]: { mediaAssetId, imageUrl }
    }));
  }

  submit(): void {
    const exam = this.exam();
    if (!exam) return;
    const answerable = answerableQuestions(exam.questions);
    this.api
      .submitExam({
        examId: exam.id,
        answers: answerable.map((q) => ({
          questionId: q.id,
          answerText: this.answers()[q.id] || '',
          answerImageMediaAssetId: this.answerImages()[q.id]?.mediaAssetId || null
        }))
      })
      .subscribe({
        next: (result) => this.result.set(result),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'play.submitExamFailed'))
      });
  }
}
