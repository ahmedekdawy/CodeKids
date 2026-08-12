import { NgStyle } from '@angular/common';
import {
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  computed,
  forwardRef,
  inject,
  input,
  signal
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslatePipe } from '../translate.pipe';

export interface MultiSelectOption {
  value: string | number;
  label: string;
}

@Component({
  selector: 'app-searchable-multi-select',
  imports: [TranslatePipe, NgStyle],
  templateUrl: './searchable-multi-select.component.html',
  styleUrl: './searchable-multi-select.component.css',
  host: {
    '[class.ms--compact]': 'compact()',
    '[class.ms--open]': 'open()',
    '[class.ms--disabled]': 'disabled()'
  },
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SearchableMultiSelectComponent),
      multi: true
    }
  ]
})
export class SearchableMultiSelectComponent implements ControlValueAccessor {
  private readonly hostEl = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly options = input.required<MultiSelectOption[]>();
  readonly placeholder = input('');
  readonly searchPlaceholder = input('');
  readonly compact = input(false);

  readonly open = signal(false);
  readonly query = signal('');
  readonly selected = signal<(string | number)[]>([]);
  readonly disabled = signal(false);
  readonly panelStyle = signal<Record<string, string>>({});

  readonly filteredOptions = computed(() => {
    const q = this.query().trim().toLowerCase();
    const opts = this.options();
    if (!q) return opts;
    return opts.filter((o) => o.label.toLowerCase().includes(q));
  });

  readonly selectedLabels = computed(() => {
    const set = new Set(this.selected().map(String));
    return this.options()
      .filter((o) => set.has(String(o.value)))
      .map((o) => o.label);
  });

  readonly summary = computed(() => {
    const labels = this.selectedLabels();
    if (!labels.length) return this.placeholder();
    if (labels.length <= 2) return labels.join(', ');
    return `${labels.slice(0, 2).join(', ')} +${labels.length - 2}`;
  });

  private onChange: (value: (string | number)[]) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  private readonly onScrollReposition = (): void => {
    if (this.open()) this.positionPanel();
  };

  constructor() {
    document.addEventListener('scroll', this.onScrollReposition, true);
    this.destroyRef.onDestroy(() => {
      document.removeEventListener('scroll', this.onScrollReposition, true);
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) return;
    if (!this.hostEl.nativeElement.contains(event.target as Node)) {
      this.close();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open()) this.close();
  }

  @HostListener('window:resize')
  onViewportChange(): void {
    if (this.open()) this.positionPanel();
  }

  writeValue(value: (string | number)[] | null): void {
    this.selected.set(Array.isArray(value) ? [...value] : []);
  }

  registerOnChange(fn: (value: (string | number)[]) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  toggleOpen(): void {
    if (this.disabled()) return;
    if (this.open()) {
      this.close();
      return;
    }
    this.query.set('');
    this.open.set(true);
    requestAnimationFrame(() => this.positionPanel());
  }

  isSelected(value: string | number): boolean {
    return this.selected().some((v) => String(v) === String(value));
  }

  toggleOption(value: string | number): void {
    if (this.disabled()) return;
    const next = this.isSelected(value)
      ? this.selected().filter((v) => String(v) !== String(value))
      : [...this.selected(), value];
    this.selected.set(next);
    this.onChange(next);
    this.onTouched();
  }

  clearAll(event: Event): void {
    event.stopPropagation();
    if (this.disabled()) return;
    this.selected.set([]);
    this.onChange([]);
    this.onTouched();
  }

  onQueryInput(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  private close(): void {
    this.open.set(false);
    this.query.set('');
    this.panelStyle.set({});
    this.onTouched();
  }

  /** Escape overflow clipping (tables / panel scroll) by pinning the menu to the viewport. */
  private positionPanel(): void {
    const trigger = this.hostEl.nativeElement.querySelector('.ms-trigger') as HTMLElement | null;
    if (!trigger) return;

    const rect = trigger.getBoundingClientRect();
    const gap = 6;
    const minWidth = this.compact() ? 14 * 16 : 16 * 16;
    const width = Math.max(rect.width, minWidth);
    const maxPanel = 22 * 16;
    const spaceBelow = window.innerHeight - rect.bottom - gap;
    const spaceAbove = rect.top - gap;
    const openUp = spaceBelow < 10 * 16 && spaceAbove > spaceBelow;
    const maxHeight = Math.max(8 * 16, Math.min(maxPanel, openUp ? spaceAbove : spaceBelow));

    let left = rect.left;
    if (left + width > window.innerWidth - 8) {
      left = Math.max(8, window.innerWidth - width - 8);
    }

    const style: Record<string, string> = {
      position: 'fixed',
      left: `${left}px`,
      right: 'auto',
      width: `${width}px`,
      zIndex: '1200',
      maxHeight: `${maxHeight}px`
    };

    if (openUp) {
      style['bottom'] = `${window.innerHeight - rect.top + gap}px`;
      style['top'] = 'auto';
    } else {
      style['top'] = `${rect.bottom + gap}px`;
      style['bottom'] = 'auto';
    }

    this.panelStyle.set(style);
  }
}
