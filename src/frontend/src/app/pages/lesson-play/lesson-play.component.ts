import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { CompleteStepResponse, Lesson, LessonStep, LessonVideoSummary } from '../../models';
import { ProtectedVideoPlayerComponent } from '../../shared/protected-video-player/protected-video-player.component';
import { StudentAskPanelComponent } from '../../shared/student-ask-panel/student-ask-panel.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';

@Component({
  selector: 'app-lesson-play',
  imports: [ReactiveFormsModule, RouterLink, ProtectedVideoPlayerComponent, StudentAskPanelComponent, TranslatePipe, ApiBusyIndicatorComponent],
  templateUrl: './lesson-play.component.html',
  styleUrl: './lesson-play.component.css'
})
export class LessonPlayComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly locale = inject(LocaleService);

  readonly lesson = signal<Lesson | null>(null);
  readonly selectedStep = signal<LessonStep | null>(null);
  readonly selectedVideo = signal<LessonVideoSummary | null>(null);
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
      this.selectedVideo.set(lesson.videos?.[0] ?? null);
    });
  }

  chooseStep(step: LessonStep): void {
    this.selectedStep.set(step);
    this.form.reset({ answer: '' });
    this.feedback.set(null);
  }

  chooseVideo(video: LessonVideoSummary): void {
    this.selectedVideo.set(video);
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

  feedbackText(result: CompleteStepResponse): string {
    return this.locale.fromApiFeedback(result);
  }
}
