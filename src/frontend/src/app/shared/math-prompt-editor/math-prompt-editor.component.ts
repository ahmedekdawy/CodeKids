import { Component, ElementRef, forwardRef, ViewChild } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslatePipe } from '../translate.pipe';

@Component({
  selector: 'app-math-prompt-editor',
  standalone: true,
  imports: [TranslatePipe],
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

  readonly symbols = ['π', '√', '∞', '±', '×', '÷', '≤', '≥', '≠', '°', '²', '³', '½', '¼'];

  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;
  disabled = false;

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

  insertFraction(): void {
    this.editor.nativeElement.focus();
    document.execCommand('insertHTML', false, '<span class="math-frac"><sup>?</sup>/<sub>?</sub></span>&nbsp;');
    this.onInput();
  }

  insertSqrt(): void {
    this.editor.nativeElement.focus();
    document.execCommand('insertText', false, '√( )');
    this.onInput();
  }

  insertPower(): void {
    this.editor.nativeElement.focus();
    document.execCommand('insertHTML', false, '<sup>?</sup>');
    this.onInput();
  }
}
