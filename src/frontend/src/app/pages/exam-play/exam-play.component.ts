import { Component, computed, inject, signal } from '@angular/core';
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
import { AttemptGuardComponent } from '../../shared/timed-attempt/attempt-guard.component';
import { TimedAttemptService } from '../../shared/timed-attempt/timed-attempt.service';
import { answerableQuestions, flattenQuestions } from '../../shared/question-draft/question-draft.util';

@Component({
  selector: 'app-exam-play',
  imports: [PageFeedbackComponent, FormsModule, RouterLink, SafeHtmlPipe, TranslatePipe, SiteBrandComponent, ApiBusyIndicatorComponent, QuestionImageDisplayComponent, QuestionPlayPromptComponent, AttemptGuardComponent],
  providers: [TimedAttemptService],
  templateUrl: './exam-play.component.html',
  styleUrl: './exam-play.component.css'
})
export class ExamPlayComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly locale = inject(LocaleService);

  readonly attempt = inject(TimedAttemptService);

  readonly exam = signal<Exam | null>(null);
  readonly result = signal<ExamAttempt | null>(null);
  readonly error = signal('');
  readonly answers = signal<Record<string, string>>({});
  readonly multiAnswers = signal<Record<string, Set<string>>>({});
  readonly answerImages = signal<Record<string, AnswerImageDraft>>({});

  /** Questions stay hidden until the student starts, so the clock matches what they can see. */
  readonly started = signal(false);
  readonly starting = signal(false);
  readonly timedOut = signal(false);

  readonly questionCount = computed(() => {
    const exam = this.exam();
    return exam ? answerableQuestions(exam.questions).length : 0;
  });

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
      },
      error: () => this.error.set(this.locale.t('play.examNotFound'))
    });
  }

  begin(): void {
    const exam = this.exam();
    if (!exam || this.started() || this.starting()) return;

    this.starting.set(true);
    this.error.set('');
    this.api.startExam(exam.id).subscribe({
      next: (attempt) => {
        this.starting.set(false);
        this.started.set(true);
        this.attempt.start({
          durationMinutes: exam.durationMinutes,
          // Anchored on the server start time so reloading the page cannot buy extra minutes.
          deadline: exam.durationMinutes
            ? Date.parse(attempt.startedAtUtc) + exam.durationMinutes * 60_000
            : null,
          onExpire: () => {
            this.timedOut.set(true);
            this.submit();
          }
        });
      },
      error: (err) => {
        this.starting.set(false);
        this.error.set(this.locale.fromApiError(err, 'play.examStartFailed'));
      }
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
    this.attempt.stop();
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
