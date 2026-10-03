import { Component, OnInit, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../translate.pipe';

export interface SetupGuideStep {
  id: string;
  path: string;
  /** No data to count: the step is done once its page was opened from the guide. */
  visitOnly?: boolean;
}

/**
 * Getting-started checklist. Text comes from `<prefix>.title|subtitle|allDone` and
 * `<prefix>.<stepId>.title|text|action|count`; a step is done when its count is above zero
 * (or, for `visitOnly` steps, once it was opened).
 */
@Component({
  selector: 'app-setup-guide',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './setup-guide.component.html',
  styleUrl: './setup-guide.component.css'
})
export class SetupGuideComponent implements OnInit {
  readonly prefix = input.required<string>();
  readonly steps = input.required<SetupGuideStep[]>();
  readonly counts = input<Record<string, number> | null>(null);

  readonly hidden = signal(false);
  private readonly visited = signal<string[]>([]);
  // undefined = nothing picked yet, so the first unfinished step is open.
  private readonly picked = signal<string | null | undefined>(undefined);

  readonly doneCount = computed(() => this.steps().filter((s) => this.isDone(s)).length);
  readonly allDone = computed(() => this.doneCount() === this.steps().length);
  readonly progress = computed(() => `${(this.doneCount() / this.steps().length) * 100}%`);
  readonly nextStep = computed(() => this.steps().find((s) => !this.isDone(s))?.id ?? null);
  readonly openStep = computed(() => {
    const picked = this.picked();
    return picked === undefined ? this.nextStep() : picked;
  });

  private get hiddenKey(): string {
    return `codekids.guide.hidden.${this.prefix()}`;
  }

  private get visitedKey(): string {
    return `codekids.guide.visited.${this.prefix()}`;
  }

  ngOnInit(): void {
    this.hidden.set(localStorage.getItem(this.hiddenKey) === '1');
    this.visited.set((localStorage.getItem(this.visitedKey) ?? '').split(',').filter(Boolean));
  }

  count(id: string): number {
    return this.counts()?.[id] ?? 0;
  }

  isDone(step: SetupGuideStep): boolean {
    return step.visitOnly ? this.visited().includes(step.id) : this.count(step.id) > 0;
  }

  markVisited(step: SetupGuideStep): void {
    if (!step.visitOnly || this.visited().includes(step.id)) return;
    this.visited.update((ids) => [...ids, step.id]);
    localStorage.setItem(this.visitedKey, this.visited().join(','));
  }

  toggle(id: string): void {
    this.picked.set(this.openStep() === id ? null : id);
  }

  setHidden(hidden: boolean): void {
    this.hidden.set(hidden);
    if (hidden) {
      localStorage.setItem(this.hiddenKey, '1');
    } else {
      localStorage.removeItem(this.hiddenKey);
    }
  }
}
