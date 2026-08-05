import { Injectable, computed, signal } from '@angular/core';
import { AR } from './translations.ar';
import { EN } from './translations.en';
import { hasApiErrorCode, resolveApiError, resolveApiFeedback } from './api-message.util';

export type AppLang = 'en' | 'ar';

const LANG_KEY = 'codekids_lang';

@Injectable({ providedIn: 'root' })
export class LocaleService {
  readonly lang = signal<AppLang>(this.readStoredLang());
  readonly isRtl = computed(() => this.lang() === 'ar');

  constructor() {
    this.applyDocument(this.lang());
  }

  setLang(lang: AppLang): void {
    if (this.lang() === lang) return;
    this.lang.set(lang);
    localStorage.setItem(LANG_KEY, lang);
    this.applyDocument(lang);
  }

  toggleLang(): void {
    this.setLang(this.lang() === 'en' ? 'ar' : 'en');
  }

  t(key: string, params?: Record<string, string | number>): string {
    const dict = this.lang() === 'ar' ? AR : EN;
    let text = dict[key] ?? EN[key] ?? key;
    if (params) {
      for (const [name, value] of Object.entries(params)) {
        text = text.replace(new RegExp(`\\{${name}\\}`, 'g'), String(value));
      }
    }
    return text;
  }

  fromApiError(err: unknown, fallbackKey?: string): string {
    this.lang();
    return resolveApiError((key, params) => this.t(key, params), err, fallbackKey);
  }

  fromApiFeedback(body: { feedbackCode?: string | null; feedback?: string | null }): string {
    this.lang();
    return resolveApiFeedback((key, params) => this.t(key, params), body);
  }

  hasApiErrorCode(err: unknown, code: string): boolean {
    return hasApiErrorCode(err, code);
  }

  private readStoredLang(): AppLang {
    const stored = localStorage.getItem(LANG_KEY);
    return stored === 'ar' ? 'ar' : 'en';
  }

  private applyDocument(lang: AppLang): void {
    const root = document.documentElement;
    root.lang = lang;
    root.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }
}
