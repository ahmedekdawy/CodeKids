import { Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '../translate.pipe';
import { TimedAttemptService } from './timed-attempt.service';

/**
 * Exam-mode chrome for a quiz or exam in progress: the countdown bar, the
 * "time is nearly up" warning, the tab-switch notice and the fullscreen lock.
 * Must be placed inside a component that provides {@link TimedAttemptService}.
 */
@Component({
  selector: 'app-attempt-guard',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './attempt-guard.component.html',
  styleUrl: './attempt-guard.component.css'
})
export class AttemptGuardComponent {
  readonly attempt = inject(TimedAttemptService);

  readonly clock = computed(() => {
    const total = Math.ceil(this.attempt.remainingMs() / 1000);
    const hours = Math.floor(total / 3600);
    const minutes = Math.floor((total % 3600) / 60);
    const seconds = total % 60;
    const pad = (value: number) => String(value).padStart(2, '0');
    return hours > 0 ? `${hours}:${pad(minutes)}:${pad(seconds)}` : `${pad(minutes)}:${pad(seconds)}`;
  });

  /** Whole minutes left, used in the warning sentence. */
  readonly warnMinutes = computed(() => Math.ceil(this.attempt.remainingMs() / 60_000));

  resumeFullscreen(): void {
    this.attempt.resumeFullscreen();
  }
}
