import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LearningApiService } from '../../learning-api.service';
import { CompleteStepResponse, Lesson, LessonStep } from '../../models';

@Component({
  selector: 'app-lesson-play',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './lesson-play.component.html',
  styleUrl: './lesson-play.component.css'
})
export class LessonPlayComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  readonly lesson = signal<Lesson | null>(null);
  readonly selectedStep = signal<LessonStep | null>(null);
  readonly feedback = signal<CompleteStepResponse | null>(null);
  readonly loading = signal(false);
  readonly form = this.fb.nonNullable.group({
    answer: ['', Validators.required]
  });

  constructor() {
    const lessonId = this.route.snapshot.paramMap.get('lessonId')!;
    this.api.getLesson(lessonId).subscribe((lesson) => {
      this.lesson.set(lesson);
      this.selectedStep.set(lesson.steps[0] ?? null);
    });
  }

  chooseStep(step: LessonStep): void {
    this.selectedStep.set(step);
    this.form.reset({ answer: '' });
    this.feedback.set(null);
  }

  submit(): void {
    const lesson = this.lesson();
    const step = this.selectedStep();
    if (!lesson || !step || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { answer } = this.form.getRawValue();
    if (!answer.trim()) {
      return;
    }

    this.loading.set(true);
    this.api.completeStep({
      lessonId: lesson.id,
      stepId: step.id,
      submittedAnswer: answer
    }).subscribe({
      next: (result) => {
        this.loading.set(false);
        this.feedback.set(result);
        if (result.isCorrect) {
          this.form.reset({ answer: '' });
        }
      },
      error: () => this.loading.set(false)
    });
  }
}
