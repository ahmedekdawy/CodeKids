import { Injectable, inject, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

const CHECK_EVERY_MS = 30 * 60 * 1000;

/**
 * The service worker keeps serving the cached app until every tab is closed, so after a deploy people
 * could run an old frontend against the new API for a long time. This notices the new version and lets
 * the page offer a refresh; it never reloads on its own, because that could interrupt an exam.
 */
@Injectable({ providedIn: 'root' })
export class PwaUpdateService {
  private readonly updates = inject(SwUpdate);

  /** True once a newer version of the app has been downloaded and is waiting for a reload. */
  readonly updateReady = signal(false);

  constructor() {
    if (!this.updates.isEnabled) return;

    this.updates.versionUpdates
      .pipe(filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'))
      .subscribe(() => this.updateReady.set(true));

    // An app left open for days would otherwise only look for updates when it is reopened.
    setInterval(() => void this.updates.checkForUpdate().catch(() => false), CHECK_EVERY_MS);
  }

  reload(): void {
    document.location.reload();
  }

  dismiss(): void {
    this.updateReady.set(false);
  }
}
