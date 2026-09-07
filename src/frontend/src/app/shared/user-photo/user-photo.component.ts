import { Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { LearningApiService } from '../../learning-api.service';

export type UserPhotoSize = 'sm' | 'md' | 'lg';

/**
 * Round profile picture. The photo endpoint requires a bearer token, so the image is
 * fetched through HttpClient (which runs the auth interceptor) and shown as an object URL
 * instead of being bound straight to `<img src>`. Falls back to the user's initials.
 */
@Component({
  selector: 'app-user-photo',
  template: `
    <span class="user-photo" [class]="size()" [attr.title]="name() || null">
      @if (blobUrl(); as src) {
        <img [src]="src" [alt]="name()" />
      } @else {
        <span class="initials" aria-hidden="true">{{ initials() }}</span>
      }
    </span>
  `,
  styleUrl: './user-photo.component.css'
})
export class UserPhotoComponent {
  private readonly http = inject(HttpClient);
  private readonly api = inject(LearningApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly photoUrl = input<string | null | undefined>(null);
  readonly name = input('');
  readonly size = input<UserPhotoSize>('md');

  readonly blobUrl = signal<string | null>(null);
  readonly initials = computed(() => {
    const words = this.name().trim().split(/\s+/).filter(Boolean);
    if (words.length === 0) return '?';
    return words.slice(0, 2).map((word) => [...word][0]).join('').toUpperCase();
  });

  private activeObjectUrl: string | null = null;

  constructor() {
    effect(() => {
      const path = this.photoUrl();
      this.clearObjectUrl();
      this.blobUrl.set(null);
      if (!path) return;

      const fullUrl = this.api.siteAssetUrl(path);
      if (!fullUrl) return;

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
