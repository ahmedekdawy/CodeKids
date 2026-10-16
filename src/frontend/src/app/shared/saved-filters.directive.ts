import { AfterContentInit, DestroyRef, Directive, contentChildren, effect, inject, output } from '@angular/core';
import { NgModel } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../auth.service';

const STORAGE_PREFIX = 'codekids_filters';

/**
 * Remembers the filters on a list page in localStorage and puts them back the next time the page opens.
 *
 * Put it on the element that holds the filter fields. Every field inside with an `ngModel` and a `name`
 * is saved, per user and page address. On return each saved value is fed through the field's own
 * `ngModelChange`, exactly as if the user picked it again, so the page reloads its list the usual way.
 * Fields are restored in page order, so a stage is set before the grade that depends on it.
 * A page opened with a query string (a link that already says what to show) is left as the link asks.
 */
@Directive({
  selector: '[appSavedFilters]'
})
export class SavedFiltersDirective implements AfterContentInit {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fields = contentChildren(NgModel, { descendants: true });

  /** Fires once saved values were put back, for pages that only apply filters on a button press. */
  readonly filtersRestored = output<void>();

  private readonly storageKey = this.buildStorageKey();
  private readonly watched = new WeakSet<NgModel>();
  private saved: Record<string, unknown> = {};
  private ready = false;

  constructor() {
    // Fields shown later (inside an @if) are picked up when they appear.
    effect(() => {
      const fields = this.fields();
      if (this.ready) this.attach(fields, false);
    });
  }

  ngAfterContentInit(): void {
    this.saved = this.read();
    // NgModel writes its first value in a microtask; restoring after it keeps the saved value on top.
    void Promise.resolve().then(() => {
      if (this.destroyRef.destroyed) return;
      this.ready = true;
      const restore = Object.keys(this.route.snapshot.queryParams).length === 0;
      const restored = this.attach(this.fields(), restore);
      if (restored) this.filtersRestored.emit();
    });
  }

  /** Watches new fields for changes and, when asked, gives them their saved value. Returns whether any was restored. */
  private attach(fields: readonly NgModel[], restore: boolean): boolean {
    let restored = false;
    for (const field of fields) {
      const name = field.name;
      if (!name || this.watched.has(field)) continue;
      this.watched.add(field);

      if (restore && Object.prototype.hasOwnProperty.call(this.saved, name)) {
        const value = this.saved[name];
        if (value !== field.value) {
          field.control.setValue(value);
          field.viewToModelUpdate(value);
          restored = true;
        }
      }

      // valueChanges also sees programmatic changes, so "clear filters" saves the cleared values too.
      const subscription = field.control.valueChanges.subscribe((value) => this.save(name, value));
      this.destroyRef.onDestroy(() => subscription.unsubscribe());
    }
    return restored;
  }

  private save(name: string, value: unknown): void {
    if (!this.storageKey) return;
    // Re-read so several filter bars on one page don't overwrite each other's values.
    const all = { ...this.read(), [name]: value ?? null };
    this.saved = all;
    try {
      localStorage.setItem(this.storageKey, JSON.stringify(all));
    } catch {
      // Storage full or blocked; the filters just won't be remembered.
    }
  }

  private read(): Record<string, unknown> {
    if (!this.storageKey) return {};
    try {
      const raw = localStorage.getItem(this.storageKey);
      const parsed = raw ? JSON.parse(raw) : null;
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {};
    } catch {
      return {};
    }
  }

  private buildStorageKey(): string {
    const user = this.auth.user();
    if (!user) return '';
    const path = this.route.snapshot.pathFromRoot
      .flatMap((route) => route.url.map((segment) => segment.path))
      .join('/');
    return `${STORAGE_PREFIX}|${user.tenantId ?? ''}|${user.id}|${path}`;
  }
}
