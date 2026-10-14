import { Injectable, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

const CHECK_EVERY_MS = 30 * 60 * 1000;
/** A new version found this soon after opening is applied at once: the user has barely started. */
const JUST_OPENED_MS = 10 * 1000;
/** Pages where a reload would lose work in progress: a lesson, quiz, assignment or exam being taken. */
const BUSY_PAGE = /^\/(lessons|quizzes|assignments|exams)\//;

/**
 * The service worker keeps serving the cached app until every tab is closed, so after a deploy people
 * could run an old frontend against the new API for a long time. This switches to the new version by
 * itself, at a moment nobody loses anything: right after opening, on the next move to another page, or
 * when the app goes to the background. Never in the middle of a lesson, quiz, assignment or exam.
 */
@Injectable({ providedIn: 'root' })
export class PwaUpdateService {
  private readonly updates = inject(SwUpdate);
  private readonly router = inject(Router);

  private updateReady = false;

  constructor() {
    if (!this.updates.isEnabled) return;

    this.updates.versionUpdates
      .pipe(filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'))
      .subscribe(() => {
        this.updateReady = true;
        if (performance.now() < JUST_OPENED_MS || document.visibilityState === 'hidden') {
          this.reloadIfIdle();
        }
      });

    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => this.reloadIfIdle());

    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') this.reloadIfIdle();
    });

    // An app left open for days would otherwise only look for updates when it is reopened.
    setInterval(() => void this.updates.checkForUpdate().catch(() => false), CHECK_EVERY_MS);
  }

  private reloadIfIdle(): void {
    if (!this.updateReady || BUSY_PAGE.test(this.router.url)) return;
    this.updateReady = false;
    document.location.reload();
  }
}
