import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, AssignmentSubmission } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-teacher-review',
  imports: [FormsModule, TranslatePipe],
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
  gradeFeedback = '';

  constructor() {
    this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
  }

  loadSubmissions(): void {
    if (!this.reviewAssignmentId) return;
    this.api.getAssignmentSubmissions(this.reviewAssignmentId).subscribe({
      next: (subs) => this.submissions.set(subs),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.review.loadFailed'))
    });
  }

  grade(submission: AssignmentSubmission): void {
    this.api
      .gradeSubmission({
        submissionId: submission.id,
        teacherFeedback: this.gradeFeedback,
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
}
