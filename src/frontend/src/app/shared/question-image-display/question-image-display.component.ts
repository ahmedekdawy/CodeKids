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
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { LearningApiService } from '../../learning-api.service';
import { TranslatePipe } from '../translate.pipe';

@Component({
  selector: 'app-question-image-display',
  imports: [TranslatePipe],
  template: `
    @if (blobUrl()) {
      @if (isPdf()) {
        <iframe
          class="question-pdf"
          [class.compact]="compact()"
          [src]="safePdfUrl()!"
          [title]="'teacher.questionAttachment.pdfViewer' | t"
        ></iframe>
      } @else {
        <img [src]="blobUrl()!" alt="" class="question-image" [class.compact]="compact()" />
      }
    }
  `,
  styleUrl: './question-image-display.component.css'
})
export class QuestionImageDisplayComponent {
  private readonly http = inject(HttpClient);
  private readonly api = inject(LearningApiService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  readonly url = input<string | null | undefined>(null);
  readonly compact = input(false);

  readonly blobUrl = signal<string | null>(null);
  readonly isPdf = signal(false);
  readonly safePdfUrl = signal<SafeResourceUrl | null>(null);
  private activeObjectUrl: string | null = null;

  constructor() {
    effect(() => {
      const path = this.url();
      this.clearObjectUrl();
      this.isPdf.set(false);
      this.safePdfUrl.set(null);
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
            const pdf = blob.type === 'application/pdf' || path.toLowerCase().includes('.pdf');
            this.isPdf.set(pdf);
            this.blobUrl.set(this.activeObjectUrl);
            if (pdf) {
              this.safePdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(this.activeObjectUrl));
            }
          },
          error: () => {
            this.clearObjectUrl();
            this.blobUrl.set(null);
            this.isPdf.set(false);
            this.safePdfUrl.set(null);
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
