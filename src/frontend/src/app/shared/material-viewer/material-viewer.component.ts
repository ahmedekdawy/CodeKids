import {
  Component,
  DestroyRef,
  computed,
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

/**
 * Shows a learning material (image, audio, or PDF) by loading it through the
 * authorized learning-materials file endpoint and displaying a local blob URL.
 */
@Component({
  selector: 'app-material-viewer',
  imports: [TranslatePipe],
  template: `
    <div class="material-viewer" [class.compact]="compact()">
      @if (loading()) {
        <p class="meta">{{ 'common.loading' | t }}</p>
      } @else if (failed()) {
        <p class="meta empty-state">{{ 'materials.previewFailed' | t }}</p>
      } @else if (blobUrl(); as url) {
        @if (kind() === 'Image') {
          <img [src]="url" [alt]="title()" class="material-image" />
        } @else if (kind() === 'Audio') {
          <audio controls [src]="url" class="material-audio"></audio>
        } @else {
          <iframe
            class="material-pdf"
            [src]="safePdfUrl()!"
            [title]="title()"
          ></iframe>
        }
      }
    </div>
  `,
  styles: [
    `
      .material-viewer {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        min-width: 0;
      }

      .material-image {
        max-width: 100%;
        max-height: 420px;
        border-radius: 12px;
        object-fit: contain;
      }

      .material-audio {
        width: 100%;
      }

      .material-pdf {
        width: 100%;
        height: 480px;
        border: 1px solid var(--border, #ddd);
        border-radius: 12px;
        background: #fff;
      }

      .compact .material-image {
        max-height: 240px;
      }

      .compact .material-pdf {
        height: 320px;
      }
    `
  ]
})
export class MaterialViewerComponent {
  private readonly http = inject(HttpClient);
  private readonly api = inject(LearningApiService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  /** The LearningMaterial id (preferred) — resolved through the authorized file endpoint. */
  readonly materialId = input<string | null | undefined>(null);
  /** Fallback: media asset id, when only the asset is known. */
  readonly mediaAssetId = input<string | null | undefined>(null);
  readonly kind = input<'Image' | 'Audio' | 'Pdf' | string>('Image');
  readonly title = input('');
  readonly compact = input(false);

  readonly blobUrl = signal<string | null>(null);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly safePdfUrl = signal<SafeResourceUrl | null>(null);

  private activeObjectUrl: string | null = null;

  readonly isPdfKind = computed(() => String(this.kind()).toLowerCase() === 'pdf');

  constructor() {
    effect(() => {
      const materialId = this.materialId();
      this.clearObjectUrl();
      this.failed.set(false);
      this.safePdfUrl.set(null);
      if (!materialId) {
        this.blobUrl.set(null);
        return;
      }

      this.loading.set(true);
      this.http
        .get(this.api.learningMaterialFileUrl(materialId), { responseType: 'blob' })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (blob) => {
            this.loading.set(false);
            this.clearObjectUrl();
            this.activeObjectUrl = URL.createObjectURL(blob);
            this.blobUrl.set(this.activeObjectUrl);
            const pdf =
              blob.type === 'application/pdf' || this.isPdfKind();
            if (pdf) {
              this.safePdfUrl.set(
                this.sanitizer.bypassSecurityTrustResourceUrl(this.activeObjectUrl)
              );
            }
          },
          error: () => {
            this.loading.set(false);
            this.clearObjectUrl();
            this.failed.set(true);
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
