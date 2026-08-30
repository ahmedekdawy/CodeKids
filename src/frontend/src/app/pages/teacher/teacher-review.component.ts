import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, AssignmentSubmission } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { QuestionImageDisplayComponent } from '../../shared/question-image-display/question-image-display.component';
import { QuestionImageUploadComponent } from '../../shared/question-image-upload/question-image-upload.component';

interface SubmissionDraft {
  feedback: string;
  feedbackImageMediaAssetId: string | null;
  feedbackImageUrl: string | null;
}

@Component({
  selector: 'app-teacher-review',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, TranslatePipe, QuestionImageDisplayComponent, QuestionImageUploadComponent],
  templateUrl: './teacher-review.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherReviewComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly assignments = signal<Assignment[]>([]);
  readonly submissions = signal<AssignmentSubmission[]>([]);
  readonly error = signal('');
  readonly info = signal('');

  reviewAssignmentId = '';
  private readonly drafts = signal<Record<string, SubmissionDraft>>({});

  constructor() {
    this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
  }

  loadSubmissions(): void {
    if (!this.reviewAssignmentId) return;
    this.api.getAssignmentSubmissions(this.reviewAssignmentId).subscribe({
      next: (subs) => {
        this.submissions.set(subs);
        const nextDrafts: Record<string, SubmissionDraft> = {};
        for (const sub of subs) {
          nextDrafts[sub.id] = {
            feedback: sub.teacherFeedback || '',
            feedbackImageMediaAssetId: null,
            feedbackImageUrl: sub.feedbackImageUrl || null
          };
        }
        this.drafts.set(nextDrafts);
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.review.loadFailed'))
    });
  }

  draftFor(submissionId: string): SubmissionDraft {
    return this.drafts()[submissionId] || { feedback: '', feedbackImageMediaAssetId: null, feedbackImageUrl: null };
  }

  setFeedback(submissionId: string, feedback: string): void {
    this.drafts.update((current) => ({
      ...current,
      [submissionId]: { ...this.draftFor(submissionId), feedback }
    }));
  }

  setFeedbackImage(submissionId: string, mediaAssetId: string | null, imageUrl: string | null): void {
    this.drafts.update((current) => ({
      ...current,
      [submissionId]: {
        ...this.draftFor(submissionId),
        feedbackImageMediaAssetId: mediaAssetId,
        feedbackImageUrl: imageUrl
      }
    }));
  }

  grade(submission: AssignmentSubmission): void {
    const draft = this.draftFor(submission.id);
    this.api
      .gradeSubmission({
        submissionId: submission.id,
        teacherFeedback: draft.feedback,
        feedbackImageMediaAssetId: draft.feedbackImageMediaAssetId,
        answers: submission.answers.map((a) => ({
          questionId: a.questionId,
          isCorrect: a.isCorrect ?? false,
          pointsAwarded: a.pointsAwarded ?? (a.isCorrect ? a.points : 0)
        }))
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.review.graded'));
          this.loadSubmissions();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.review.gradeFailed'))
      });
  }

  markAnswer(submissionId: string, questionId: string, correct: boolean): void {
    const subs = this.submissions().map((s) => {
      if (s.id !== submissionId) return s;
      return {
        ...s,
        answers: s.answers.map((a) =>
          a.questionId === questionId
            ? { ...a, isCorrect: correct, pointsAwarded: correct ? a.points : 0 }
            : a
        )
      };
    });
    this.submissions.set(subs);
  }

  setPoints(submissionId: string, questionId: string, points: number): void {
    const subs = this.submissions().map((s) => {
      if (s.id !== submissionId) return s;
      return {
        ...s,
        answers: s.answers.map((a) => {
          if (a.questionId !== questionId) return a;
          const awarded = Math.max(0, Math.min(a.points, points));
          return {
            ...a,
            pointsAwarded: awarded,
            isCorrect: awarded >= a.points
          };
        })
      };
    });
    this.submissions.set(subs);
  }
}
