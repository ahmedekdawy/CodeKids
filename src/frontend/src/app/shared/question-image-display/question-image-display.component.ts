import {
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { LearningApiService } from '../../learning-api.service';

@Component({
  selector: 'app-question-image-display',
  template: `
    @if (blobUrl()) {
      <img [src]="blobUrl()!" alt="" class="question-image" [class.compact]="compact()" />
    }
  `,
  styleUrl: './question-image-display.component.css'
})
export class QuestionImageDisplayComponent {
  private readonly http = inject(HttpClient);
  private readonly api = inject(LearningApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly url = input<string | null | undefined>(null);
  readonly compact = input(false);

  readonly blobUrl = signal<string | null>(null);
  private activeObjectUrl: string | null = null;

  constructor() {
    effect(() => {
      const path = this.url();
      this.clearObjectUrl();
      if (!path) {
        this.blobUrl.set(null);
        return;
      }

      const fullUrl = this.api.siteAssetUrl(path);
      if (!fullUrl) {
        this.blobUrl.set(null);
        return;
      }

      this.http
        .get(fullUrl, { responseType: 'blob' })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (blob) => {
            this.clearObjectUrl();
            this.activeObjectUrl = URL.createObjectURL(blob);
            this.blobUrl.set(this.activeObjectUrl);
          },
          error: () => {
            this.clearObjectUrl();
            this.blobUrl.set(null);
          }
        });
    });

    this.destroyRef.onDestroy(() => this.clearObjectUrl());
  }

  private clearObjectUrl(): void {
    if (this.activeObjectUrl) {
      URL.revokeObjectURL(this.activeObjectUrl);
      this.activeObjectUrl = null;
    }
  }
}
