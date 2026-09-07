import { Injectable, OnDestroy, computed, signal } from '@angular/core';

/** Minutes left when the student is told the time is nearly up. Mirrors AssessmentDuration.WarnMinutes. */
export const ATTEMPT_WARN_MINUTES = 10;

export interface TimedAttemptOptions {
  /** Time allowed for the whole attempt. Null or zero runs the attempt untimed. */
  durationMinutes?: number | null;
  /** Epoch ms the attempt must end at. Wins over durationMinutes so a reload cannot reset the clock. */
  deadline?: number | null;
  /** Called once when the clock reaches zero. */
  onExpire: () => void;
}

/**
 * Owns the countdown and the exam-mode restrictions for a single quiz or exam attempt.
 * Provide it on the play component (not in root) so each attempt gets a fresh instance.
 */
@Injectable()
export class TimedAttemptService implements OnDestroy {
  private ticker: ReturnType<typeof setInterval> | null = null;
  private readonly detach: Array<() => void> = [];
  private deadline = 0;
  private onExpire: (() => void) | null = null;

  /** True between start() and stop(), i.e. while the student is solving. */
  readonly running = signal(false);
  /** True when the teacher put a time limit on this assessment. */
  readonly timed = signal(false);
  readonly remainingMs = signal(0);
  private readonly warnAtMs = signal(0);

  /** Number of times the student left the tab or window while solving. */
  readonly tabSwitches = signal(0);
  readonly fullscreen = signal(false);

  /** Fullscreen is only enforced where the browser actually allows it. */
  readonly fullscreenSupported = signal(false);

  /** The attempt is on screen but the student dropped out of fullscreen. */
  readonly fullscreenLost = computed(
    () => this.running() && this.fullscreenSupported() && !this.fullscreen()
  );

  readonly warning = computed(
    () => this.running() && this.timed() && this.remainingMs() > 0 && this.remainingMs() <= this.warnAtMs()
  );

  start(options: TimedAttemptOptions): void {
    this.stop();
    this.onExpire = options.onExpire;

    const limitMs = (options.durationMinutes ?? 0) > 0 ? options.durationMinutes! * 60_000 : 0;
    this.deadline = options.deadline ?? (limitMs > 0 ? Date.now() + limitMs : 0);
    this.timed.set(this.deadline > 0);
    // A quiz shorter than the warning window still deserves a heads-up, so fall back to half the time.
    this.warnAtMs.set(Math.min(ATTEMPT_WARN_MINUTES * 60_000, Math.floor(limitMs / 2) || ATTEMPT_WARN_MINUTES * 60_000));

    this.running.set(true);
    this.watchBrowser();
    this.enterFullscreen();
    this.ticker = setInterval(() => this.tick(), 1000);
    // Tick last: an already-expired deadline stops the attempt, and stop() must find the timer to clear.
    this.tick();
  }

  /** Ends the attempt: clears the clock, drops the listeners and leaves fullscreen. */
  stop(): void {
    if (this.ticker) {
      clearInterval(this.ticker);
      this.ticker = null;
    }
    while (this.detach.length) this.detach.pop()!();
    this.onExpire = null;
    this.running.set(false);
    this.exitFullscreen();
  }

  /** Re-enters fullscreen after the student dismissed it. Must be called from a click handler. */
  resumeFullscreen(): void {
    this.enterFullscreen();
  }

  ngOnDestroy(): void {
    this.stop();
  }

  private tick(): void {
    if (!this.timed()) return;
    const remaining = Math.max(0, this.deadline - Date.now());
    this.remainingMs.set(remaining);
    if (remaining > 0) return;

    const expire = this.onExpire;
    this.stop();
    expire?.();
  }

  private watchBrowser(): void {
    const onVisibility = () => {
      if (document.hidden) this.tabSwitches.update((count) => count + 1);
    };
    const onFullscreenChange = () => this.fullscreen.set(!!document.fullscreenElement);
    const onBeforeUnload = (event: BeforeUnloadEvent) => event.preventDefault();

    document.addEventListener('visibilitychange', onVisibility);
    document.addEventListener('fullscreenchange', onFullscreenChange);
    window.addEventListener('beforeunload', onBeforeUnload);

    this.detach.push(
      () => document.removeEventListener('visibilitychange', onVisibility),
      () => document.removeEventListener('fullscreenchange', onFullscreenChange),
      () => window.removeEventListener('beforeunload', onBeforeUnload)
    );
  }

  private enterFullscreen(): void {
    if (!document.fullscreenEnabled || !document.documentElement.requestFullscreen) return;
    document.documentElement
      .requestFullscreen({ navigationUI: 'hide' })
      .then(() => {
        // The attempt can end while the request is still pending (e.g. an already-expired deadline).
        if (!this.running()) {
          this.exitFullscreen();
          return;
        }
        this.fullscreenSupported.set(true);
        this.fullscreen.set(true);
      })
      // Browsers refuse outside a user gesture; keep solving unlocked rather than trapping the student.
      .catch(() => this.fullscreen.set(!!document.fullscreenElement));
  }

  private exitFullscreen(): void {
    this.fullscreen.set(false);
    if (document.fullscreenElement) document.exitFullscreen().catch(() => undefined);
  }
}
