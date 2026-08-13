import { Component, effect, inject, input } from '@angular/core';
import { ToastService } from '../toast/toast.service';

@Component({
  selector: 'app-page-feedback',
  template: `
    @if (ok()) {
      <p class="feedback ok">{{ ok() }}</p>
    }
    @if (error()) {
      <p class="feedback">{{ error() }}</p>
    }
  `
})
export class PageFeedbackComponent {
  readonly ok = input('');
  readonly error = input('');
  private readonly toasts = inject(ToastService);

  constructor() {
    effect(() => {
      const text = this.ok().trim();
      if (text) this.toasts.success(text);
    });
    effect(() => {
      const text = this.error().trim();
      if (text) this.toasts.error(text);
    });
  }
}
