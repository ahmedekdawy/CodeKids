import { Injectable, inject, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'chat' | 'notification';

export interface ToastItem {
  id: number;
  kind: ToastKind;
  text: string;
  title?: string;
  href?: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  readonly items = signal<ToastItem[]>([]);

  success(text: string): void {
    this.push({ kind: 'success', text });
  }

  error(text: string): void {
    this.push({ kind: 'error', text });
  }

  chat(title: string, text: string, href: string): void {
    this.push({ kind: 'chat', title, text, href });
  }

  notification(title: string, text: string, href: string): void {
    this.push({ kind: 'notification', title, text, href });
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((item) => item.id !== id));
  }

  private push(item: Omit<ToastItem, 'id'>): void {
    const trimmed = (item.text ?? '').trim();
    if (!trimmed && !item.title) return;
    const id = this.nextId++;
    this.items.update((list) => [...list, { ...item, id, text: trimmed }]);
    window.setTimeout(() => this.dismiss(id), item.kind === 'chat' || item.kind === 'notification' ? 12000 : 5200);
  }
}
