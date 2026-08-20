import {
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  computed,
  forwardRef,
  inject,
  input,
  signal,
  viewChild
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslatePipe } from '../translate.pipe';
import {
  applyDropdownPanelStyle,
  attachDropdownPanelToBody,
  isInsideDropdown
} from '../dropdown-panel.util';

export interface MultiSelectOption {
  value: string | number;
  label: string;
}

@Component({
  selector: 'app-searchable-multi-select',
  imports: [TranslatePipe],
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
  private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');

  readonly options = input.required<MultiSelectOption[]>();
  readonly placeholder = input('');
  readonly searchPlaceholder = input('');
  readonly compact = input(false);

  readonly open = signal(false);
  readonly query = signal('');
  readonly selected = signal<(string | number)[]>([]);
  readonly disabled = signal(false);

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

  private readonly onPointerDown = (event: PointerEvent): void => {
    if (!this.open()) return;
    if (isInsideDropdown(event.target, this.hostEl.nativeElement, this.panel()?.nativeElement)) {
      return;
    }
    this.close();
  };

  constructor() {
    document.addEventListener('scroll', this.onScrollReposition, true);
    document.addEventListener('pointerdown', this.onPointerDown, true);
    this.destroyRef.onDestroy(() => {
      document.removeEventListener('scroll', this.onScrollReposition, true);
      document.removeEventListener('pointerdown', this.onPointerDown, true);
    });
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
    requestAnimationFrame(() => {
      this.positionPanel();
      this.panel()?.nativeElement.querySelector<HTMLInputElement>('.ms-search')?.focus();
    });
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
    this.onTouched();
  }

  private positionPanel(): void {
    const panel =
      this.panel()?.nativeElement ??
      (this.hostEl.nativeElement.querySelector('.ms-panel') as HTMLElement | null);
    const trigger = this.hostEl.nativeElement.querySelector('.ms-trigger') as HTMLElement | null;
    if (!panel || !trigger) return;
    attachDropdownPanelToBody(panel);
    applyDropdownPanelStyle(panel, trigger, this.compact());
  }
}
