import { Component, inject, input, output, signal } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { TranslatePipe } from '../translate.pipe';
import { QuestionImageDisplayComponent } from '../question-image-display/question-image-display.component';

@Component({
  selector: 'app-question-image-upload',
  imports: [TranslatePipe, QuestionImageDisplayComponent],
  templateUrl: './question-image-upload.component.html',
  styleUrl: './question-image-upload.component.css'
})
export class QuestionImageUploadComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly mediaAssetId = input<string | null>(null);
  readonly imageUrl = input<string | null>(null);
  readonly mediaAssetIdChange = output<string | null>();
  readonly imageUrlChange = output<string | null>();

  readonly uploading = signal(false);
  readonly error = signal('');

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.error.set('');
    this.uploading.set(true);
    this.api.uploadQuestionImage(file).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.mediaAssetIdChange.emit(result.id);
        this.imageUrlChange.emit(result.url);
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(this.locale.fromApiError(err, 'teacher.questionImage.uploadFailed'));
      }
    });
  }

  clear(): void {
    this.mediaAssetIdChange.emit(null);
    this.imageUrlChange.emit(null);
    this.error.set('');
  }
}
