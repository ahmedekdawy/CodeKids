import { Component, inject, input } from '@angular/core';
import { ApiBusyService } from '../../api-busy.service';
import { TranslatePipe } from '../translate.pipe';

@Component({
  selector: 'app-api-busy-indicator',
  imports: [TranslatePipe],
  template: `
    @if (busy() ?? apiBusy.busy()) {
      <div class="api-busy" role="status" aria-live="polite" aria-busy="true">
        <span class="api-busy-spinner" aria-hidden="true"></span>
        <p>{{ 'common.loading' | t }}</p>
      </div>
    }
  `,
  styles: `
    :host {
      display: contents;
    }

    .api-busy {
      position: absolute;
      inset: 0;
      z-index: 20;
      display: grid;
      place-content: center;
      justify-items: center;
      gap: 0.65rem;
      pointer-events: none;
      background: rgba(7, 17, 31, 0.28);
    }

    .api-busy p {
      margin: 0;
      padding: 0.35rem 0.7rem;
      border-radius: var(--radius-pill);
      background: var(--bg-elevated);
      border: 1px solid var(--border);
      color: var(--text);
      font-weight: 600;
      font-size: 0.92rem;
    }

    .api-busy-spinner {
      width: 1.85rem;
      height: 1.85rem;
      border-radius: 50%;
      border: 3px solid rgba(255, 214, 10, 0.22);
      border-top-color: var(--accent);
      animation: api-spin 0.7s linear infinite;
    }

    @keyframes api-spin {
      to {
        transform: rotate(360deg);
      }
    }
  `
})
export class ApiBusyIndicatorComponent {
  readonly apiBusy = inject(ApiBusyService);
  /** When set, ignore global HTTP busy and show only for this flag (e.g. login submit). */
  readonly busy = input<boolean | undefined>(undefined);
}
