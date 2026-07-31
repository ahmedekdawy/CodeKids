import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LearningApiService } from '../../learning-api.service';
import { Quiz, SubmitQuizResponse } from '../../models';

@Component({
  selector: 'app-quiz-play',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './quiz-play.component.html',
  styleUrl: './quiz-play.component.css'
})
export class QuizPlayComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  readonly quiz = signal<Quiz | null>(null);
  readonly result = signal<SubmitQuizResponse | null>(null);
  readonly loading = signal(false);
  form: FormGroup | null = null;

  constructor() {
    const quizId = this.route.snapshot.paramMap.get('quizId')!;
    this.api.getQuiz(quizId).subscribe((quiz) => {
      this.quiz.set(quiz);
      this.form = this.fb.group(
        Object.fromEntries(
          quiz.questions.map((question) => [
            question.id,
            this.fb.control('', Validators.required)
          ])
        )
      );
    });
  }

  submit(): void {
    const quiz = this.quiz();
    if (!quiz || !this.form || this.form.invalid) {
      this.form?.markAllAsTouched();
      return;
    }

    const answers = this.form.getRawValue() as Record<string, string>;
    this.loading.set(true);
    this.api.submitQuiz({
      quizId: quiz.id,
      answers: quiz.questions.map((question) => ({
        questionId: question.id,
        selectedOption: answers[question.id] || ''
      }))
    }).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.result.set(response);
      },
      error: () => this.loading.set(false)
    });
  }
}
