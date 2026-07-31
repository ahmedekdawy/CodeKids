import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, AssignmentSubmission } from '../../models';

@Component({
  selector: 'app-assignment-play',
  imports: [FormsModule, RouterLink],
  templateUrl: './assignment-play.component.html',
  styleUrl: './assignment-play.component.css'
})
export class AssignmentPlayComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);

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
      error: () => this.error.set('Assignment not found.')
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
        error: (err) => this.error.set(err?.error?.message || 'Could not submit assignment.')
      });
  }
}
