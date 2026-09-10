import { Component, ElementRef, forwardRef, ViewChild } from '@angular/core';
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
  @ViewChild('editor', { static: true }) editor!: ElementRef<HTMLDivElement>;
  @ViewChild('fracNumInput') fracNumInput?: ElementRef<HTMLInputElement>;

  readonly formatTools: { cmd: string; titleKey: string; label: string }[] = [
    { cmd: 'bold', titleKey: 'editor.bold', label: 'B' },
    { cmd: 'italic', titleKey: 'editor.italic', label: 'I' },
    { cmd: 'underline', titleKey: 'editor.underline', label: 'U' },
    { cmd: 'strikeThrough', titleKey: 'editor.strikethrough', label: 'S' },
    { cmd: 'subscript', titleKey: 'editor.subscript', label: 'X₂' },
    { cmd: 'superscript', titleKey: 'editor.superscript', label: 'X²' },
    { cmd: 'removeFormat', titleKey: 'editor.clearFormat', label: 'Tx' }
  ];

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
    this.onChange(this.editor.nativeElement.innerHTML);
  }

  onBlur(): void {
    this.onTouched();
  }

  selectGroup(id: string): void {
    this.activeGroupId = id;
  }

  exec(command: string, value?: string): void {
    this.editor.nativeElement.focus();
    document.execCommand(command, false, value);
    this.onInput();
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

  private saveSelection(): void {
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
