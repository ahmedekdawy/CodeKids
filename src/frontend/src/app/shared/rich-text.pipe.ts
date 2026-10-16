import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

/** Formatting the answer editor produces; anything else is dropped (its text is kept). */
const ALLOWED_TAGS = new Set([
  'B', 'STRONG', 'I', 'EM', 'U', 'S', 'STRIKE', 'SUB', 'SUP', 'SPAN', 'DIV', 'P', 'BR', 'FONT',
  'UL', 'OL', 'LI', 'SMALL', 'MARK'
]);
const ALLOWED_STYLES = new Set([
  'color', 'background-color', 'font-family', 'font-size', 'font-weight', 'font-style',
  'text-decoration', 'text-decoration-line', 'text-align', 'vertical-align', 'direction'
]);
/** Only plain values: no url(), expression() or other ways to load or run something. */
const SAFE_STYLE_VALUE = /^[#\w\s.,%()'"-]*$/;
const UNSAFE_STYLE_VALUE = /url\s*\(|expression\s*\(|javascript:/i;

/**
 * Renders HTML written by a student (or any untrusted author) keeping only text formatting.
 * Unlike safeHtml, which trusts its input, this rebuilds the markup from an allow-list, so scripts,
 * event handlers, links and images cannot get through. Plain text passes through unchanged.
 */
@Pipe({ name: 'richText', standalone: true })
export class RichTextPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(value: string | null | undefined): SafeHtml {
    const html = value ?? '';
    if (!/[<&]/.test(html)) {
      return html;
    }

    const source = new DOMParser().parseFromString(`<div>${html}</div>`, 'text/html').body.firstElementChild;
    const target = document.createElement('div');
    if (source) {
      copyAllowed(source, target);
    }
    return this.sanitizer.bypassSecurityTrustHtml(target.innerHTML);
  }
}

function copyAllowed(from: Element, to: Element): void {
  for (const node of Array.from(from.childNodes)) {
    if (node.nodeType === Node.TEXT_NODE) {
      to.appendChild(document.createTextNode(node.textContent ?? ''));
      continue;
    }
    if (node.nodeType !== Node.ELEMENT_NODE) {
      continue;
    }

    const element = node as Element;
    if (element.tagName === 'SCRIPT' || element.tagName === 'STYLE') {
      continue;
    }
    if (!ALLOWED_TAGS.has(element.tagName)) {
      copyAllowed(element, to);
      continue;
    }

    const clone = document.createElement(element.tagName.toLowerCase());
    const className = element.getAttribute('class');
    if (className && /^[\w\s-]+$/.test(className)) {
      clone.setAttribute('class', className);
    }
    const dir = element.getAttribute('dir');
    if (dir === 'rtl' || dir === 'ltr' || dir === 'auto') {
      clone.setAttribute('dir', dir);
    }
    const style = safeStyle(element.getAttribute('style'));
    if (style) {
      clone.setAttribute('style', style);
    }
    copyAllowed(element, clone);
    to.appendChild(clone);
  }
}

function safeStyle(style: string | null): string {
  if (!style) return '';
  return style
    .split(';')
    .map((rule) => rule.split(':'))
    .filter(([name, ...rest]) => {
      const value = rest.join(':').trim();
      return (
        rest.length > 0 &&
        ALLOWED_STYLES.has(name.trim().toLowerCase()) &&
        SAFE_STYLE_VALUE.test(value) &&
        !UNSAFE_STYLE_VALUE.test(value)
      );
    })
    .map(([name, ...rest]) => `${name.trim().toLowerCase()}: ${rest.join(':').trim()}`)
    .join('; ');
}
