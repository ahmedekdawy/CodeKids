import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, AssignmentSubmission } from '../../models';
import { ProtectedVideoPlayerComponent } from '../../shared/protected-video-player/protected-video-player.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-assignment-play',
  imports: [FormsModule, RouterLink, ProtectedVideoPlayerComponent, TranslatePipe, SiteBrandComponent],
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

  constructor() {
    const id = this.route.snapshot.paramMap.get('assignmentId');
    if (!id) return;
    this.api.getAssignment(id).subscribe({
      next: (assignment) => {
        this.assignment.set(assignment);
        const seed: Record<string, string> = {};
        for (const q of assignment.questions) seed[q.id] = '';
        this.answers.set(seed);
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

  submit(): void {
    const assignment = this.assignment();
    if (!assignment) return;
    this.api
      .submitAssignment({
        assignmentId: assignment.id,
        answers: assignment.questions.map((q) => ({
          questionId: q.id,
          answerText: this.answers()[q.id] || ''
        }))
      })
      .subscribe({
        next: (result) => this.result.set(result),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'play.submitAssignmentFailed'))
      });
  }
}
