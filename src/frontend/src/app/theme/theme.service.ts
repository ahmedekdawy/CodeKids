import { Injectable, signal } from '@angular/core';

export type AppTheme = 'dark' | 'light';

const THEME_KEY = 'codekids_theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<AppTheme>(this.readStoredTheme());

  constructor() {
    this.applyDocument(this.theme());
  }

  setTheme(theme: AppTheme): void {
    if (this.theme() === theme) return;
    this.theme.set(theme);
    localStorage.setItem(THEME_KEY, theme);
    this.applyDocument(theme);
  }

  toggleTheme(): void {
    this.setTheme(this.theme() === 'dark' ? 'light' : 'dark');
  }

  private readStoredTheme(): AppTheme {
    return localStorage.getItem(THEME_KEY) === 'light' ? 'light' : 'dark';
  }

  private applyDocument(theme: AppTheme): void {
    const root = document.documentElement;
    if (theme === 'light') {
      root.dataset['theme'] = 'light';
    } else {
      delete root.dataset['theme'];
    }
  }
}
