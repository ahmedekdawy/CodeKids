import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ApiBusyService {
  private readonly pending = signal(0);
  readonly busy = computed(() => this.pending() > 0);

  begin(): void {
    this.pending.update((count) => count + 1);
  }

  end(): void {
    this.pending.update((count) => Math.max(0, count - 1));
  }
}
