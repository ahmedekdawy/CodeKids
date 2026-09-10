import { booleanAttribute, Component, ElementRef, HostListener, Input, forwardRef, inject, ViewChild } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslatePipe } from '../translate.pipe';

export interface MathSymbolGroup {
  id: string;
  labelKey: string;
  symbols: string[];
}

@Component({
  selector: 'app-math-prompt-editor',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  templateUrl: './math-prompt-editor.component.html',
  styleUrl: './math-prompt-editor.component.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MathPromptEditorComponent),
      multi: true
    }
  ]
})
export class MathPromptEditorComponent implements ControlValueAccessor {
  private readonly host = inject(ElementRef<HTMLElement>);

  @ViewChild('editor', { static: true }) editor!: ElementRef<HTMLDivElement>;
  @ViewChild('fracNumInput') fracNumInput?: ElementRef<HTMLInputElement>;

  @Input({ transform: booleanAttribute }) compact = false;
  @Input() placeholderKey = 'editor.promptPlaceholder';

  readonly formatTools: { cmd: string; titleKey: string; label: string }[] = [
    { cmd: 'bold', titleKey: 'editor.bold', label: 'B' },
    { cmd: 'italic', titleKey: 'editor.italic', label: 'I' },
    { cmd: 'underline', titleKey: 'editor.underline', label: 'U' },
    { cmd: 'strikeThrough', titleKey: 'editor.strikethrough', label: 'S' },
    { cmd: 'subscript', titleKey: 'editor.subscript', label: 'X₂' },
    { cmd: 'superscript', titleKey: 'editor.superscript', label: 'X²' }
  ];

  readonly alignTools: { cmd: string; titleKey: string; label: string }[] = [
    { cmd: 'justifyLeft', titleKey: 'editor.alignLeft', label: '⬅' },
    { cmd: 'justifyCenter', titleKey: 'editor.alignCenter', label: '☰' },
    { cmd: 'justifyRight', titleKey: 'editor.alignRight', label: '➡' }
  ];

  readonly fontFamilies: { value: string; labelKey: string; preview: string }[] = [
    { value: '', labelKey: 'editor.fontDefault', preview: 'inherit' },
    { value: 'Cairo, "Segoe UI", Tahoma, sans-serif', labelKey: 'editor.fontCairo', preview: 'Cairo, sans-serif' },
    { value: 'Arial, Helvetica, sans-serif', labelKey: 'editor.fontArial', preview: 'Arial, sans-serif' },
    { value: 'Tahoma, Geneva, sans-serif', labelKey: 'editor.fontTahoma', preview: 'Tahoma, sans-serif' },
    { value: '"Times New Roman", Times, serif', labelKey: 'editor.fontTimes', preview: '"Times New Roman", serif' },
    { value: 'Georgia, "Times New Roman", serif', labelKey: 'editor.fontGeorgia', preview: 'Georgia, serif' },
    { value: '"Courier New", Courier, monospace', labelKey: 'editor.fontCourier', preview: '"Courier New", monospace' },
    { value: 'Verdana, Geneva, sans-serif', labelKey: 'editor.fontVerdana', preview: 'Verdana, sans-serif' }
  ];

  readonly fontSizes: { value: string; label: string }[] = [
    { value: '', label: '—' },
    { value: '12px', label: '12' },
    { value: '14px', label: '14' },
    { value: '16px', label: '16' },
    { value: '18px', label: '18' },
    { value: '20px', label: '20' },
    { value: '24px', label: '24' },
    { value: '28px', label: '28' },
    { value: '32px', label: '32' },
    { value: '36px', label: '36' }
  ];

  selectedFontFamily = '';
  selectedFontSize = '';

  readonly textColors = [
    '#111827',
    '#b91c1c',
    '#c2410c',
    '#a16207',
    '#15803d',
    '#0f766e',
    '#1d4ed8',
    '#6d28d9',
    '#be185d',
    '#ffffff'
  ];

  readonly highlightColors = [
    '#fef08a',
    '#bbf7d0',
    '#bae6fd',
    '#fbcfe8',
    '#fed7aa',
    '#ddd6fe',
    '#e5e7eb'
  ];

  colorPanel: 'text' | 'highlight' | null = null;

  readonly equationTools: { id: string; titleKey: string; label: string }[] = [
    { id: 'fraction', titleKey: 'editor.fraction', label: 'a/b' },
    { id: 'sqrt', titleKey: 'editor.sqrt', label: '√' },
    { id: 'nroot', titleKey: 'editor.nroot', label: 'ⁿ√' },
    { id: 'power', titleKey: 'editor.power', label: 'xⁿ' },
    { id: 'index', titleKey: 'editor.index', label: 'xₙ' },
    { id: 'abs', titleKey: 'editor.abs', label: '|x|' },
    { id: 'parens', titleKey: 'editor.parens', label: '( )' },
    { id: 'brackets', titleKey: 'editor.brackets', label: '[ ]' },
    { id: 'braces', titleKey: 'editor.braces', label: '{ }' }
  ];

  readonly symbolGroups: MathSymbolGroup[] = [
    {
      id: 'ops',
      labelKey: 'editor.group.operators',
      symbols: ['+', '−', '±', '∓', '×', '÷', '·', '∗', '∘', '∕', '=', '≠', '≈', '≡', '∝', '∞', '%', '‰']
    },
    {
      id: 'rel',
      labelKey: 'editor.group.relations',
      symbols: ['<', '>', '≤', '≥', '≪', '≫', '≲', '≳', '≃', '≅', '∼', '≔', '≠', '∝']
    },
    {
      id: 'greek',
      labelKey: 'editor.group.greek',
      symbols: [
        'α', 'β', 'γ', 'δ', 'ε', 'ζ', 'η', 'θ', 'ι', 'κ', 'λ', 'μ', 'ν', 'ξ', 'π', 'ρ', 'σ', 'ς', 'τ', 'υ', 'φ', 'χ', 'ψ', 'ω',
        'Α', 'Β', 'Γ', 'Δ', 'Ε', 'Ζ', 'Η', 'Θ', 'Ι', 'Κ', 'Λ', 'Μ', 'Ν', 'Ξ', 'Π', 'Ρ', 'Σ', 'Τ', 'Υ', 'Φ', 'Χ', 'Ψ', 'Ω'
      ]
    },
    {
      id: 'sets',
      labelKey: 'editor.group.sets',
      symbols: ['∈', '∉', '∋', '⊂', '⊃', '⊆', '⊇', '∪', '∩', '∅', 'ℕ', 'ℤ', 'ℚ', 'ℝ', 'ℂ', '∀', '∃', '∄', '∧', '∨', '¬', '⇒', '⇔']
    },
    {
      id: 'arrows',
      labelKey: 'editor.group.arrows',
      symbols: ['→', '←', '↔', '⇒', '⇐', '⇔', '↑', '↓', '↦', '↪', '↩', '⟶', '⟵']
    },
    {
      id: 'calc',
      labelKey: 'editor.group.calculus',
      symbols: ['∑', '∏', '∫', '∬', '∮', '∂', '∇', '∆', '′', '″', '‴', 'lim', 'dx', 'dy', 'dθ']
    },
    {
      id: 'geo',
      labelKey: 'editor.group.geometry',
      symbols: ['°', '′', '″', '∠', '∟', '⊥', '∥', '△', '□', '◯', '≅', '∼', '≈']
    },
    {
      id: 'misc',
      labelKey: 'editor.group.misc',
      symbols: ['√', '∛', '∜', '²', '³', 'ⁿ', '₁', '₂', '₃', '½', '⅓', '¼', '¾', '…', '∴', '∵', '⊥', '⊤', '♯', '♭']
    }
  ];

  activeGroupId = this.symbolGroups[0].id;
  showFractionBuilder = false;
  fractionNum = '';
  fractionDen = '';

  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;
  private savedRange: Range | null = null;
  disabled = false;

  get activeGroup(): MathSymbolGroup {
    return this.symbolGroups.find(g => g.id === this.activeGroupId) ?? this.symbolGroups[0];
  }

  writeValue(value: string | null): void {
    const html = value || '';
    if (this.editor?.nativeElement && this.editor.nativeElement.innerHTML !== html) {
      this.editor.nativeElement.innerHTML = html;
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    if (this.editor?.nativeElement) {
      this.editor.nativeElement.contentEditable = isDisabled ? 'false' : 'true';
    }
  }

  onInput(): void {
    const html = this.editor.nativeElement.innerHTML;
    this.onChange(this.isEffectivelyEmpty(html) ? '' : html);
  }

  onBlur(): void {
    this.onTouched();
  }

  selectGroup(id: string): void {
    this.activeGroupId = id;
  }

  exec(command: string, value?: string): void {
    this.colorPanel = null;
    this.editor.nativeElement.focus();
    document.execCommand('styleWithCSS', false, 'true');
    document.execCommand(command, false, value);
    this.onInput();
  }

  toggleColorPanel(kind: 'text' | 'highlight'): void {
    this.saveSelection();
    this.colorPanel = this.colorPanel === kind ? null : kind;
  }

  applyForeColor(color: string): void {
    this.restoreSelection();
    document.execCommand('styleWithCSS', false, 'true');
    document.execCommand('foreColor', false, color);
    this.colorPanel = null;
    this.onInput();
  }

  applyDefaultTextColor(): void {
    this.restoreSelection();
    document.execCommand('styleWithCSS', false, 'true');
    document.execCommand('foreColor', false, 'inherit');
    this.unwrapInheritColor();
    this.colorPanel = null;
    this.onInput();
  }

  applyHighlight(color: string): void {
    this.restoreSelection();
    document.execCommand('styleWithCSS', false, 'true');
    if (!document.execCommand('hiliteColor', false, color)) {
      document.execCommand('backColor', false, color);
    }
    this.colorPanel = null;
    this.onInput();
  }

  clearHighlight(): void {
    this.applyHighlight('transparent');
  }

  applyFontFamily(family: string): void {
    this.selectedFontFamily = family;
    this.colorPanel = null;
    this.editor.nativeElement.focus();
    this.restoreSelection();
    document.execCommand('styleWithCSS', false, 'true');
    if (!family) {
      document.execCommand('fontName', false, 'inherit');
      this.normalizeFontNameSpans();
      this.onInput();
      return;
    }
    document.execCommand('fontName', false, family);
    this.normalizeFontNameSpans();
    this.onInput();
  }

  applyFontSize(size: string): void {
    this.selectedFontSize = size;
    this.colorPanel = null;
    this.editor.nativeElement.focus();
    this.restoreSelection();
    if (!size) {
      this.clearInlineFontSize();
      this.onInput();
      return;
    }
    document.execCommand('styleWithCSS', false, 'true');
    document.execCommand('fontSize', false, '7');
    this.replaceTempFontSize(size);
    this.onInput();
  }

  onFontFamilyChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.saveSelection();
    this.applyFontFamily(value);
  }

  onFontSizeChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.saveSelection();
    this.applyFontSize(value);
  }

  @HostListener('document:mousedown', ['$event'])
  onDocumentMouseDown(event: MouseEvent): void {
    if (!this.colorPanel) return;
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.colorPanel = null;
    }
  }

  saveSelection(): void {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) {
      this.savedRange = null;
      return;
    }
    const range = selection.getRangeAt(0);
    if (this.editor.nativeElement.contains(range.commonAncestorContainer)) {
      this.savedRange = range.cloneRange();
    } else {
      this.savedRange = null;
    }
  }

  insertSymbol(symbol: string): void {
    this.editor.nativeElement.focus();
    document.execCommand('insertText', false, symbol);
    this.onInput();
  }

  runEquationTool(id: string): void {
    if (id === 'fraction') {
      this.openFractionBuilder();
      return;
    }

    switch (id) {
      case 'sqrt':
        this.insertHtml(
          '<span class="math-sqrt"><span class="math-sqrt-radic" aria-hidden="true">√</span><span class="math-sqrt-body">?</span></span>&nbsp;'
        );
        break;
      case 'nroot':
        this.insertHtml(
          '<span class="math-nroot"><sup class="math-nroot-index">n</sup><span class="math-sqrt"><span class="math-sqrt-radic" aria-hidden="true">√</span><span class="math-sqrt-body">?</span></span></span>&nbsp;'
        );
        break;
      case 'power':
        this.insertHtml('<sup>?</sup>');
        break;
      case 'index':
        this.insertHtml('<sub>?</sub>');
        break;
      case 'abs':
        this.insertHtml('<span class="math-abs">|?|</span>&nbsp;');
        break;
      case 'parens':
        this.insertText('( )');
        break;
      case 'brackets':
        this.insertText('[ ]');
        break;
      case 'braces':
        this.insertText('{ }');
        break;
    }
  }

  openFractionBuilder(): void {
    this.saveSelection();
    this.showFractionBuilder = true;
    this.fractionNum = '';
    this.fractionDen = '';
    setTimeout(() => this.fracNumInput?.nativeElement.focus(), 0);
  }

  cancelFractionBuilder(): void {
    this.showFractionBuilder = false;
    this.fractionNum = '';
    this.fractionDen = '';
  }

  insertFraction(): void {
    const num = this.escapeHtml(this.fractionNum.trim() || '?');
    const den = this.escapeHtml(this.fractionDen.trim() || '?');
    this.insertHtml(
      `<span class="math-frac" contenteditable="false">` +
        `<span class="math-num">${num}</span>` +
        `<span class="math-den">${den}</span>` +
      `</span>\u00A0`
    );
    this.cancelFractionBuilder();
  }

  private unwrapInheritColor(): void {
    this.editor.nativeElement.querySelectorAll('span, font').forEach((node) => {
      const el = node as HTMLElement;
      const color = (el.style?.color || el.getAttribute('color') || '').toLowerCase();
      if (color !== 'inherit') return;
      el.style.removeProperty('color');
      el.removeAttribute('color');
      if (!el.getAttribute('style') && !el.className && !el.getAttribute('class')) {
        el.replaceWith(...Array.from(el.childNodes));
      }
    });
  }

  private normalizeFontNameSpans(): void {
    this.editor.nativeElement.querySelectorAll('font[face], span[style*="font-family"]').forEach((node) => {
      const el = node as HTMLElement;
      const face = el.getAttribute('face') || el.style.fontFamily || '';
      if (!face || face.toLowerCase() === 'inherit') {
        el.style.removeProperty('font-family');
        el.removeAttribute('face');
        if (el.tagName === 'FONT' || (!el.getAttribute('style') && !el.className)) {
          el.replaceWith(...Array.from(el.childNodes));
        }
        return;
      }
      if (el.tagName === 'FONT') {
        const span = document.createElement('span');
        span.style.fontFamily = face;
        while (el.firstChild) span.appendChild(el.firstChild);
        el.replaceWith(span);
      }
    });
  }

  private replaceTempFontSize(size: string): void {
    const root = this.editor.nativeElement;
    root.querySelectorAll('font[size="7"]').forEach((node) => {
      const font = node as HTMLElement;
      const span = document.createElement('span');
      span.style.fontSize = size;
      while (font.firstChild) span.appendChild(font.firstChild);
      font.replaceWith(span);
    });
    root.querySelectorAll('span').forEach((node) => {
      const el = node as HTMLElement;
      const current = (el.style.fontSize || '').toLowerCase();
      if (
        current === 'xxx-large' ||
        current === '-webkit-xxx-large' ||
        current === 'xx-large' ||
        current.includes('xxx-large')
      ) {
        el.style.fontSize = size;
      }
    });
  }

  private clearInlineFontSize(): void {
    document.execCommand('styleWithCSS', false, 'true');
    document.execCommand('fontSize', false, '7');
    const root = this.editor.nativeElement;
    root.querySelectorAll('font[size="7"]').forEach((node) => {
      const font = node as HTMLElement;
      font.replaceWith(...Array.from(font.childNodes));
    });
    root.querySelectorAll('span').forEach((node) => {
      const el = node as HTMLElement;
      const current = (el.style.fontSize || '').toLowerCase();
      if (
        current === 'xxx-large' ||
        current === '-webkit-xxx-large' ||
        current === 'xx-large' ||
        current.includes('xxx-large')
      ) {
        el.style.removeProperty('font-size');
        if (!el.getAttribute('style') && !el.className) {
          el.replaceWith(...Array.from(el.childNodes));
        }
      }
    });
  }

  private isEffectivelyEmpty(html: string): boolean {
    const text = (this.editor.nativeElement.textContent || '').replace(/\u00a0/g, ' ').trim();
    if (text.length > 0) return false;
    return !/math-|style=|<img\b|<svg\b/i.test(html);
  }

  private restoreSelection(): void {
    this.editor.nativeElement.focus();
    const selection = window.getSelection();
    if (!selection) return;
    selection.removeAllRanges();
    if (this.savedRange) {
      selection.addRange(this.savedRange);
    } else {
      const range = document.createRange();
      range.selectNodeContents(this.editor.nativeElement);
      range.collapse(false);
      selection.addRange(range);
    }
  }

  private insertHtml(html: string): void {
    this.restoreSelection();
    const selection = window.getSelection();
    const template = document.createElement('template');
    template.innerHTML = html;
    const fragment = template.content;

    if (!selection || selection.rangeCount === 0) {
      this.editor.nativeElement.appendChild(fragment);
      this.onInput();
      this.savedRange = null;
      return;
    }

    const range = selection.getRangeAt(0);
    range.deleteContents();
    const lastNode = fragment.lastChild;
    range.insertNode(fragment);

    if (lastNode) {
      range.setStartAfter(lastNode);
      range.collapse(true);
      selection.removeAllRanges();
      selection.addRange(range);
    }

    this.onInput();
    this.savedRange = null;
  }

  private insertText(text: string): void {
    this.editor.nativeElement.focus();
    document.execCommand('insertText', false, text);
    this.onInput();
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }
}
