import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error';

export interface ToastItem {
  id: number;
  kind: ToastKind;
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  readonly items = signal<ToastItem[]>([]);

  success(text: string): void {
    this.push('success', text);
  }

  error(text: string): void {
    this.push('error', text);
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((item) => item.id !== id));
  }

  private push(kind: ToastKind, text: string): void {
    const trimmed = text.trim();
    if (!trimmed) return;
    const id = this.nextId++;
    this.items.update((list) => [...list, { id, kind, text: trimmed }]);
    window.setTimeout(() => this.dismiss(id), 5200);
  }
}
