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

export interface SelectOption {
  value: string | number | null;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-searchable-select',
  imports: [TranslatePipe],
  templateUrl: './searchable-select.component.html',
  styleUrl: './searchable-select.component.css',
  host: {
    '[class.ss--compact]': 'compact()',
    '[class.ss--open]': 'open()',
    '[class.ss--disabled]': 'isDisabled()'
  },
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SearchableSelectComponent),
      multi: true
    }
  ]
})
export class SearchableSelectComponent implements ControlValueAccessor {
  private readonly hostEl = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);
  private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');

  readonly options = input<SelectOption[]>([]);
  readonly placeholder = input('');
  readonly searchPlaceholder = input('');
  readonly emptyLabel = input('');
  readonly emptyValue = input<string | number | null>('');
  readonly compact = input(false);
  readonly disabled = input(false);

  readonly open = signal(false);
  readonly query = signal('');
  readonly selected = signal<string | number | null>('');
  readonly cvaDisabled = signal(false);

  readonly isDisabled = computed(() => this.disabled() || this.cvaDisabled());

  readonly allOptions = computed((): SelectOption[] => {
    const emptyLabel = this.emptyLabel().trim();
    const opts = this.options();
    if (!emptyLabel) return opts;
    return [{ value: this.emptyValue(), label: emptyLabel }, ...opts];
  });

  readonly filteredOptions = computed(() => {
    const q = this.query().trim().toLowerCase();
    const opts = this.allOptions();
    if (!q) return opts;
    return opts.filter((o) => o.label.toLowerCase().includes(q));
  });

  readonly selectedLabel = computed(() => {
    const current = this.selected();
    const match = this.allOptions().find((o) => this.sameValue(o.value, current));
    if (match) return match.label;
    if (current === '' || current === null || current === undefined) {
      return this.emptyLabel() || this.placeholder();
    }
    return String(current);
  });

  readonly isPlaceholder = computed(() => {
    const current = this.selected();
    return this.sameValue(current, this.emptyValue()) || current === '' || current === null;
  });

  private onChange: (value: string | number | null) => void = () => undefined;
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

  writeValue(value: string | number | null): void {
    this.selected.set(value ?? this.emptyValue());
  }

  registerOnChange(fn: (value: string | number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.cvaDisabled.set(isDisabled);
  }

  toggleOpen(): void {
    if (this.isDisabled()) return;
    if (this.open()) {
      this.close();
      return;
    }
    this.query.set('');
    this.open.set(true);
    requestAnimationFrame(() => {
      this.positionPanel();
      this.panel()?.nativeElement.querySelector<HTMLInputElement>('.ss-search')?.focus();
    });
  }

  isSelected(value: string | number | null): boolean {
    return this.sameValue(this.selected(), value);
  }

  selectOption(option: SelectOption): void {
    if (this.isDisabled() || option.disabled) return;
    this.selected.set(option.value);
    this.onChange(option.value);
    this.onTouched();
    this.close();
  }

  onQueryInput(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  private sameValue(a: string | number | null | undefined, b: string | number | null | undefined): boolean {
    if (a === b) return true;
    if ((a === '' || a === null || a === undefined) && (b === '' || b === null || b === undefined)) {
      return true;
    }
    return String(a) === String(b);
  }

  private close(): void {
    this.open.set(false);
    this.query.set('');
    this.onTouched();
  }

  private positionPanel(): void {
    const panel =
      this.panel()?.nativeElement ??
      (this.hostEl.nativeElement.querySelector('.ss-panel') as HTMLElement | null);
    const trigger = this.hostEl.nativeElement.querySelector('.ss-trigger') as HTMLElement | null;
    if (!panel || !trigger) return;
    attachDropdownPanelToBody(panel);
    applyDropdownPanelStyle(panel, trigger, this.compact());
  }
}
