import { Component, ElementRef, HostListener, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { AppNotification } from '../../models';
import { NotificationNotifyService } from '../../notification-notify.service';
import { NotificationRealtimeService } from '../../notification-realtime.service';
import { TranslatePipe } from '../translate.pipe';

@Component({
  selector: 'app-notification-bell',
  imports: [TranslatePipe],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.css'
})
export class NotificationBellComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly api = inject(LearningApiService);
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);
  private readonly notify = inject(NotificationNotifyService);
  private readonly realtime = inject(NotificationRealtimeService);
  private readonly router = inject(Router);

  readonly open = signal(false);
  readonly loading = signal(false);
  readonly unreadCount = signal(0);
  readonly items = signal<AppNotification[]>([]);

  constructor() {
    this.reload();
    this.realtime.notifications$.subscribe((notification) => {
      this.unreadCount.update((count) => count + (notification.isRead ? 0 : 1));
      this.items.update((list) => {
        const next = [notification, ...list.filter((x) => x.id !== notification.id)];
        return next.slice(0, 30);
      });
    });
  }

  toggle(event: Event): void {
    event.stopPropagation();
    const next = !this.open();
    this.open.set(next);
    if (next) {
      this.reload();
    }
  }

  reload(): void {
    if (!this.auth.token()) return;
    this.loading.set(true);
    this.api.listNotifications().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
    this.api.getNotificationUnreadSummary().subscribe({
      next: (summary) => this.unreadCount.set(summary.unreadCount)
    });
  }

  label(item: AppNotification): string {
    return this.notify.displayBody(item);
  }

  openItem(item: AppNotification, event: Event): void {
    event.stopPropagation();
    this.open.set(false);
    if (!item.isRead) {
      this.api.markNotificationRead(item.id).subscribe({
        next: (updated) => {
          this.items.update((list) => list.map((x) => (x.id === updated.id ? updated : x)));
          this.unreadCount.update((count) => Math.max(0, count - 1));
        }
      });
    }
    if (item.targetUrl) {
      void this.router.navigateByUrl(item.targetUrl);
    }
  }

  markAllRead(event: Event): void {
    event.stopPropagation();
    this.api.markAllNotificationsRead().subscribe({
      next: () => {
        this.unreadCount.set(0);
        this.items.update((list) => list.map((x) => ({ ...x, isRead: true })));
      }
    });
  }

  formatWhen(value: string): string {
    try {
      return new Intl.DateTimeFormat(this.locale.lang(), {
        dateStyle: 'short',
        timeStyle: 'short'
      }).format(new Date(value));
    } catch {
      return value;
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) return;
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }
}
