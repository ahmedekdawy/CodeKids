import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { SearchableSelectComponent } from '../searchable-select/searchable-select.component';
import { TranslatePipe } from '../translate.pipe';

export interface StudentAskCourseChoice {
  id: string;
  title: string;
}

@Component({
  selector: 'app-student-ask-panel',
  imports: [FormsModule, RouterLink, TranslatePipe, SearchableSelectComponent],
  templateUrl: './student-ask-panel.component.html',
  styleUrl: './student-ask-panel.component.css'
})
export class StudentAskPanelComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly courseId = input<string | null>(null);
  readonly unitId = input<string | null>(null);
  readonly lessonId = input<string | null>(null);
  readonly titleKey = input('studentAsk.title');
  readonly courseChoices = input<StudentAskCourseChoice[]>([]);

  question = '';
  readonly open = signal(false);
  readonly selectedCourseId = signal('');
  readonly loading = signal(false);
  readonly error = signal('');
  readonly answer = signal('');
  readonly inScope = signal(true);

  toggle(): void {
    this.open.update((value) => !value);
    this.ensureCourse();
  }

  close(): void {
    this.open.set(false);
  }

  onCourseSelected(value: string | number | null): void {
    this.selectedCourseId.set(value == null ? '' : String(value));
  }

  submit(): void {
    const text = this.question.trim();
    if (!text) {
      this.error.set(this.locale.t('studentAsk.questionRequired'));
      return;
    }

    this.ensureCourse();
    const courseId = this.selectedCourseId() || this.courseId();
    if (!courseId && !this.lessonId() && !this.unitId()) {
      this.error.set(this.locale.t('studentAsk.pickCourse'));
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.api
      .askStudentQuestion({
        question: text,
        courseId,
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

  private ensureCourse(): void {
    if (this.selectedCourseId()) return;
    const fromInput = this.courseId();
    if (fromInput) {
      this.selectedCourseId.set(fromInput);
      return;
    }
    const first = this.courseChoices()[0]?.id;
    if (first) this.selectedCourseId.set(first);
  }
}
