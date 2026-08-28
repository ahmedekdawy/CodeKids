import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { TranslatePipe } from '../translate.pipe';

@Component({
  selector: 'app-student-ask-panel',
  imports: [FormsModule, RouterLink, TranslatePipe],
  template: `
    <section class="block student-ask">
      <h3>{{ titleKey() | t }}</h3>
      <p class="meta">{{ 'studentAsk.hint' | t }}</p>
      <form (ngSubmit)="submit()">
        <textarea
          [(ngModel)]="question"
          name="studentAskQuestion"
          rows="4"
          [placeholder]="'studentAsk.placeholder' | t"
        ></textarea>
        <button type="submit" [disabled]="loading()">{{ 'studentAsk.submit' | t }}</button>
      </form>
      <p class="meta"><a routerLink="/student/asked-questions">{{ 'askedQuestions.seeAll' | t }}</a></p>
      @if (error()) {
        <p class="meta student-ask-error">{{ error() }}</p>
      }
      @if (answer(); as text) {
        <div class="student-ask-answer" [class.out]="!inScope()">
          <strong>{{ inScope() ? ('studentAsk.answer' | t) : ('studentAsk.outOfScope' | t) }}</strong>
          <p>{{ text }}</p>
        </div>
      }
    </section>
  `,
  styles: `
    .student-ask {
      display: grid;
      gap: 0.65rem;
    }
    .student-ask textarea {
      width: 100%;
      margin-bottom: 0.55rem;
    }
    .student-ask-answer {
      border: 1px solid var(--border);
      border-radius: 10px;
      padding: 0.7rem 0.8rem;
    }
    .student-ask-answer.out {
      opacity: 0.9;
    }
    .student-ask-error {
      color: var(--danger, #b42318);
    }
  `
})
export class StudentAskPanelComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly courseId = input<string | null>(null);
  readonly unitId = input<string | null>(null);
  readonly lessonId = input<string | null>(null);
  readonly titleKey = input('studentAsk.title');

  question = '';
  readonly loading = signal(false);
  readonly error = signal('');
  readonly answer = signal('');
  readonly inScope = signal(true);

  submit(): void {
    const text = this.question.trim();
    if (!text) {
      this.error.set(this.locale.t('studentAsk.questionRequired'));
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.api
      .askStudentQuestion({
        question: text,
        courseId: this.courseId(),
        unitId: this.unitId(),
        lessonId: this.lessonId()
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.inScope.set(result.inScope);
          this.answer.set(result.answer);
          if (result.inScope) {
            this.question = '';
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.detail || this.locale.t('studentAsk.failed'));
        }
      });
  }
}
