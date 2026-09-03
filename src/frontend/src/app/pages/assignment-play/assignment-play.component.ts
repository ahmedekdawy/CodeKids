import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, AssignmentSubmission } from '../../models';
import { ProtectedVideoPlayerComponent } from '../../shared/protected-video-player/protected-video-player.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { QuestionImageDisplayComponent } from '../../shared/question-image-display/question-image-display.component';
import { QuestionPlayPromptComponent } from '../../shared/question-play-prompt/question-play-prompt.component';
import { AnswerImageDraft } from '../../shared/question-play-prompt/playable-question';
import { answerableQuestions, flattenQuestions } from '../../shared/question-draft/question-draft.util';

@Component({
  selector: 'app-assignment-play',
  imports: [PageFeedbackComponent, FormsModule, RouterLink, ProtectedVideoPlayerComponent, TranslatePipe, SiteBrandComponent, ApiBusyIndicatorComponent, QuestionImageDisplayComponent, QuestionPlayPromptComponent],
  templateUrl: './assignment-play.component.html',
  styleUrl: './assignment-play.component.css'
})
export class AssignmentPlayComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly locale = inject(LocaleService);

  readonly assignment = signal<Assignment | null>(null);
  readonly result = signal<AssignmentSubmission | null>(null);
  readonly error = signal('');
  readonly answers = signal<Record<string, string>>({});
  readonly multiAnswers = signal<Record<string, Set<string>>>({});
  readonly answerImages = signal<Record<string, AnswerImageDraft>>({});

  constructor() {
    const id = this.route.snapshot.paramMap.get('assignmentId');
    if (!id) return;
    this.api.getAssignment(id).subscribe({
      next: (assignment) => {
        this.assignment.set(assignment);
        const seed: Record<string, string> = {};
        const multi: Record<string, Set<string>> = {};
        const images: Record<string, AnswerImageDraft> = {};
        for (const q of flattenQuestions(assignment.questions)) {
          seed[q.id] = '';
          if (q.questionType === 'MultiChoice') multi[q.id] = new Set();
          images[q.id] = { mediaAssetId: null, imageUrl: null };
        }
        this.answers.set(seed);
        this.multiAnswers.set(multi);
        this.answerImages.set(images);
      },
      error: () => this.error.set(this.locale.t('play.assignmentNotFound'))
    });
  }

  submittedScore(submission: AssignmentSubmission): string {
    return this.locale.t('play.submittedScore', {
      status: submission.status,
      score: submission.score ?? this.locale.t('common.pending'),
      max: submission.maxScore ?? this.locale.t('common.emDash')
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

  setAnswerImage(questionId: string, mediaAssetId: string | null, imageUrl: string | null): void {
    this.answerImages.update((current) => ({
      ...current,
      [questionId]: { mediaAssetId, imageUrl }
    }));
  }

  submit(): void {
    const assignment = this.assignment();
    if (!assignment) return;
    this.api
      .submitAssignment({
        assignmentId: assignment.id,
        answers: answerableQuestions(assignment.questions).map((q) => ({
          questionId: q.id,
          answerText: this.answers()[q.id] || '',
          answerImageMediaAssetId: this.answerImages()[q.id]?.mediaAssetId || null
        }))
      })
      .subscribe({
        next: (result) => this.result.set(result),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'play.submitAssignmentFailed'))
      });
  }
}
