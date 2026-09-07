import { Component, inject, input, output, signal } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { TranslatePipe } from '../translate.pipe';
import { QuestionImageDisplayComponent } from '../question-image-display/question-image-display.component';

@Component({
  selector: 'app-student-answer-upload',
  imports: [QuestionImageDisplayComponent, TranslatePipe],
  templateUrl: './student-answer-upload.component.html',
  styleUrl: './student-answer-upload.component.css'
})
export class StudentAnswerUploadComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly mediaAssetId = input<string | null>(null);
  readonly imageUrl = input<string | null>(null);
  readonly disabled = input(false);

  readonly mediaAssetIdChange = output<string | null>();
  readonly imageUrlChange = output<string | null>();

  readonly uploading = signal(false);
  readonly error = signal('');

  onFileSelected(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    if (!file) return;

    this.uploading.set(true);
    this.error.set('');
    this.api.uploadQuestionImage(file).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.mediaAssetIdChange.emit(result.id);
        this.imageUrlChange.emit(result.url);
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(this.locale.fromApiError(err, 'student.answerImage.uploadFailed'));
      }
    });
  }

  clear(): void {
    this.mediaAssetIdChange.emit(null);
    this.imageUrlChange.emit(null);
  }
}
